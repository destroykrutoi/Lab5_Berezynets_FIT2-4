using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _theme;
    private readonly IAppStateService _state;
    private readonly IDiagnosticsService _diagnostics;
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(IThemeService theme, IAppStateService state, IDiagnosticsService diagnostics,
        ILogger<SettingsViewModel> logger)
    {
        _theme = theme;
        _state = state;
        _diagnostics = diagnostics;
        _logger = logger;
        UpdateText();
    }

    [ObservableProperty]
    private string _currentThemeText = string.Empty;

    [ObservableProperty]
    private string _diagnosticsMessage = string.Empty;

    public string SessionInfo =>
        $"Сесію розпочато о {_state.SessionStartedAt:HH:mm:ss}, повернень із фону: {_state.ResumeCount}";

    public string CrashReportingText => _diagnostics.IsCrashReportingEnabled
        ? $"Збір крашів: Sentry увімкнено ({SentryConfig.Release})"
        : "Збір крашів: Sentry не налаштовано (немає DSN)";

    [RelayCommand]
    private void SetTheme(string name)
    {
        if (Enum.TryParse(name, out AppTheme theme))
        {
            _theme.SetTheme(theme);
            UpdateText();
        }
    }

    // Моделювання 1: виняток у фоновій задачі, результат якої ніхто не очікує
    [RelayCommand]
    private async Task SimulateTaskErrorAsync()
    {
        _logger.LogInformation("Моделювання: запуск фонової задачі, яка завершиться винятком");
        StartForgottenTask();

        // UnobservedTaskException виникає, коли збирач сміття фіналізує задачу з винятком
        await Task.Delay(500);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        DiagnosticsMessage = "Фонова задача завершилась винятком — запис ERR у журналі (вкладка «Логи»)";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StartForgottenTask() => Task.Run(RecalculateScheduleInBackground);

    private static void RecalculateScheduleInBackground() =>
        throw new InvalidOperationException("Змодельований збій: не вдалося перерахувати графік руху поїздів у фоновій задачі");

    // Моделювання 2: необроблений виняток у фоновому потоці — аварійне завершення застосунку
    [RelayCommand]
    private void SimulateCrash()
    {
        _logger.LogWarning("Моделювання: аварійне завершення через необроблений виняток у потоці TelemetryWorker");
        _diagnostics.TrackEvent("CrashSimulated");
        var thread = new Thread(ProcessTelemetry) { IsBackground = true, Name = "TelemetryWorker" };
        thread.Start();
    }

    private static void ProcessTelemetry() =>
        throw new InvalidOperationException("Змодельований краш: необроблений виняток під час обробки телеметрії поїзда");

    public void Refresh()
    {
        UpdateText();
        OnPropertyChanged(nameof(SessionInfo));
        OnPropertyChanged(nameof(CrashReportingText));
    }

    private void UpdateText() =>
        CurrentThemeText = _theme.CurrentTheme switch
        {
            AppTheme.Light => "Поточна тема: світла",
            AppTheme.Dark => "Поточна тема: темна",
            _ => "Поточна тема: як у системі"
        };
}
