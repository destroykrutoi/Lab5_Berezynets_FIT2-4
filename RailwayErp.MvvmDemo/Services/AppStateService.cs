using Microsoft.Extensions.Logging;

namespace RailwayErp.MvvmDemo.Services;

public interface IAppStateService
{
    DateTime SessionStartedAt { get; }
    bool IsInBackground { get; }
    string CurrentPage { get; set; }
    int ResumeCount { get; }

    event EventHandler? AppStopped;
    event EventHandler? AppResumed;

    void Log(string message, LogLevel level = LogLevel.Information);
    void NotifyStopped();
    void NotifyResumed();
}

// Сервіс стану сесії: реєструється як Singleton, тому живе весь час роботи застосунку
public class AppStateService : IAppStateService
{
    private readonly ILogger<AppStateService> _logger;

    public AppStateService(ILogger<AppStateService> logger)
    {
        _logger = logger;
        SessionStartedAt = DateTime.Now;
    }

    public DateTime SessionStartedAt { get; }
    public bool IsInBackground { get; private set; }
    public string CurrentPage { get; set; } = string.Empty;
    public int ResumeCount { get; private set; }

    public event EventHandler? AppStopped;
    public event EventHandler? AppResumed;

    // події життєвого циклу записуються в загальний журнал Serilog
    public void Log(string message, LogLevel level = LogLevel.Information) =>
        _logger.Log(level, "{Message}", message);

    public void NotifyStopped()
    {
        IsInBackground = true;
        AppStopped?.Invoke(this, EventArgs.Empty);
    }

    public void NotifyResumed()
    {
        IsInBackground = false;
        ResumeCount++;
        AppResumed?.Invoke(this, EventArgs.Empty);
    }
}
