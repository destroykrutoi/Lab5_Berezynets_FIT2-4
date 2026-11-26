using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo;

public partial class App : Application
{
    private readonly IAppStateService _state;
    private readonly IDeepLinkService _deepLinks;

    public App(IAppStateService state, IThemeService theme, IDeepLinkService deepLinks)
    {
        InitializeComponent();
        _state = state;
        _deepLinks = deepLinks;

        theme.ApplySaved(); // тема, обрана користувачем раніше
        MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Title = "RailwayErp.MvvmDemo";
        window.Width = 560;
        window.Height = 820;

        window.Created += async (s, e) =>
        {
            _state.Log("Window.Created — застосунок створено");
            await Task.Delay(500);
            await _deepLinks.HandlePendingAsync();
        };
        window.Activated += (s, e) => _state.Log("Window.Activated — застосунок на передньому плані", LogLevel.Debug);
        window.Deactivated += (s, e) => _state.Log("Window.Deactivated — застосунок втратив фокус", LogLevel.Debug);
        window.Stopped += (s, e) =>
        {
            _state.Log("Window.Stopped — застосунок згорнуто у фон");
            _state.NotifyStopped();
        };
        window.Resumed += (s, e) =>
        {
            _state.Log("Window.Resumed — застосунок відновлено з фону");
            _state.NotifyResumed();
        };
        window.Destroying += (s, e) => _state.Log("Window.Destroying — завершення роботи, звільнення ресурсів");

        return window;
    }

    // Точка входу для App Links на Android/iOS; на Windows викликається з платформного коду
    protected override async void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        await _deepLinks.HandleAsync(uri);
    }
}
