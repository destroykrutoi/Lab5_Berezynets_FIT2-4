using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Serilog.Events;

namespace RailwayErp.MvvmDemo.Services;

public record LogRecord(DateTime Timestamp, LogEventLevel Level, string Source, string Message, string Exception)
{
    public string LevelText => LogReaderService.ShortName(Level);
    public string Header => $"{Timestamp:dd.MM HH:mm:ss.fff}  [{LevelText}]  {Source}";
    public bool HasException => Exception.Length > 0;
    public string ExceptionShort => Exception.Split('\n')[0].Trim();

    public Color LevelColor => Level switch
    {
        LogEventLevel.Fatal => Color.FromArgb("#B00020"),
        LogEventLevel.Error => Color.FromArgb("#D32F2F"),
        LogEventLevel.Warning => Color.FromArgb("#E65100"),
        LogEventLevel.Information => Color.FromArgb("#1565C0"),
        _ => Color.FromArgb("#757575")
    };
}

public interface ILogReaderService
{
    string CurrentFileName { get; }
    Task<List<LogRecord>> ReadLatestAsync(int count, LogEventLevel minimumLevel);
}

// Читання файлів журналу Serilog (app-yyyyMMdd.log) для екрана перегляду логів
public partial class LogReaderService : ILogReaderService
{
    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(\w{3})\] ([^:]*): (.*)$")]
    private static partial Regex LineRegex();

    public string CurrentFileName =>
        GetFiles().Select(Path.GetFileName).FirstOrDefault() ?? "—";

    public Task<List<LogRecord>> ReadLatestAsync(int count, LogEventLevel minimumLevel) =>
        Task.Run(() =>
        {
            var result = new List<LogRecord>();
            // від найновішого файлу до старіших, доки не набереться count записів
            foreach (var file in GetFiles())
            {
                var records = Parse(ReadShared(file)).Where(r => r.Level >= minimumLevel).ToList();
                result.InsertRange(0, records);
                if (result.Count >= count) break;
            }
            return result.TakeLast(count).Reverse().ToList();
        });

    private static IEnumerable<string> GetFiles() =>
        Directory.Exists(LoggingSetup.LogDirectory)
            ? Directory.GetFiles(LoggingSetup.LogDirectory, "app-*.log").OrderByDescending(f => f)
            : Enumerable.Empty<string>();

    // файл одночасно відкритий Serilog на запис, тому читаємо з FileShare.ReadWrite
    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static IEnumerable<LogRecord> Parse(string text)
    {
        LogRecord? current = null;
        var exception = new StringBuilder();

        foreach (var line in text.Split('\n'))
        {
            var match = LineRegex().Match(line.TrimEnd('\r'));
            if (match.Success)
            {
                if (current is not null)
                    yield return current with { Exception = exception.ToString().Trim() };
                exception.Clear();
                current = new LogRecord(
                    DateTime.ParseExact(match.Groups[1].Value, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    ParseLevel(match.Groups[2].Value),
                    ShortSource(match.Groups[3].Value),
                    match.Groups[4].Value,
                    string.Empty);
            }
            else if (current is not null && line.Trim().Length > 0)
            {
                // рядки стека викликів належать до попереднього запису
                exception.AppendLine(line.TrimEnd('\r'));
            }
        }

        if (current is not null)
            yield return current with { Exception = exception.ToString().Trim() };
    }

    private static string ShortSource(string source) =>
        source.Contains('.') ? source[(source.LastIndexOf('.') + 1)..] : source;

    private static LogEventLevel ParseLevel(string s) => s switch
    {
        "VRB" => LogEventLevel.Verbose,
        "DBG" => LogEventLevel.Debug,
        "INF" => LogEventLevel.Information,
        "WRN" => LogEventLevel.Warning,
        "ERR" => LogEventLevel.Error,
        _ => LogEventLevel.Fatal
    };

    public static string ShortName(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "VRB",
        LogEventLevel.Debug => "DBG",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        _ => "FTL"
    };
}
