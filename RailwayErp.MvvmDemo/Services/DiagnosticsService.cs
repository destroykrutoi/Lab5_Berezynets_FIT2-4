using System.Text;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace RailwayErp.MvvmDemo.Services;

public interface IDiagnosticsService
{
    bool IsCrashReportingEnabled { get; }
    void TrackEvent(string name, IDictionary<string, string>? properties = null);
    Task<string> SendBugReportAsync();
}

// Аналітика використання та ручний звіт про помилку (Sentry + локальний файл)
public class DiagnosticsService : IDiagnosticsService
{
    private readonly ILogger<DiagnosticsService> _logger;
    private readonly ILogReaderService _logs;

    public DiagnosticsService(ILogger<DiagnosticsService> logger, ILogReaderService logs)
    {
        _logger = logger;
        _logs = logs;
    }

    public bool IsCrashReportingEnabled => SentrySdk.IsEnabled;

    public void TrackEvent(string name, IDictionary<string, string>? properties = null)
    {
        var data = properties is null ? string.Empty : string.Join(", ", properties.Select(p => $"{p.Key}={p.Value}"));
        _logger.LogInformation("Analytics: {EventName} {EventData}", name, data);
        // подія аналітики потрапляє в хронологію (breadcrumbs) наступного звіту Sentry
        SentrySdk.AddBreadcrumb(name, "analytics", "user", properties?.ToDictionary(p => p.Key, p => p.Value));
    }

    public async Task<string> SendBugReportAsync()
    {
        var recent = await _logs.ReadLatestAsync(50, LogEventLevel.Debug);
        var report = new StringBuilder()
            .AppendLine("Звіт про помилку RailwayErp.MvvmDemo")
            .AppendLine($"Час: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            .AppendLine($"Версія застосунку: {AppInfo.VersionString} (збірка {AppInfo.BuildString})")
            .AppendLine($"Платформа: {DeviceInfo.Platform} {DeviceInfo.VersionString}")
            .AppendLine($"Пристрій: {DeviceInfo.Manufacturer} {DeviceInfo.Model} ({DeviceInfo.Idiom})")
            .AppendLine()
            .AppendLine($"Останні {recent.Count} записів журналу:");
        foreach (var r in Enumerable.Reverse(recent))
        {
            report.AppendLine($"{r.Timestamp:HH:mm:ss.fff} [{r.LevelText}] {r.Source}: {r.Message}");
            if (r.HasException) report.AppendLine("    " + r.ExceptionShort);
        }

        var dir = Path.Combine(FileSystem.AppDataDirectory, "reports");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"bug-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        await File.WriteAllTextAsync(file, report.ToString(), Encoding.UTF8);

        if (!SentrySdk.IsEnabled)
        {
            _logger.LogWarning("Звіт про помилку збережено локально ({File}), Sentry не налаштовано", Path.GetFileName(file));
            return $"Звіт збережено локально: {Path.GetFileName(file)}";
        }

        var bytes = Encoding.UTF8.GetBytes(report.ToString());
        var id = SentrySdk.CaptureMessage("Звіт користувача про помилку", scope =>
        {
            scope.Level = SentryLevel.Warning;
            scope.SetTag("report", "manual");
            scope.AddAttachment(bytes, "recent-log.txt", AttachmentType.Default, "text/plain");
        });
        await SentrySdk.FlushAsync(TimeSpan.FromSeconds(5));
        _logger.LogWarning("Користувач надіслав звіт про помилку, Sentry EventId = {EventId}", id);
        return $"Звіт надіслано (EventId {id.ToString()[..8]}) і збережено: {Path.GetFileName(file)}";
    }
}
