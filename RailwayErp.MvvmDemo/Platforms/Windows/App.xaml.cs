using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Microsoft.Windows.AppLifecycle;
using RailwayErp.MvvmDemo.Services;
using Windows.ApplicationModel.Activation;

namespace RailwayErp.MvvmDemo.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();

        // платформо-специфічні винятки UI-потоку WinUI (аналог AndroidEnvironment.UnhandledExceptionRaiser)
        UnhandledException += (s, e) =>
            Serilog.Log.ForContext("SourceContext", "WinUI")
                .Fatal(e.Exception, "Необроблений виняток у UI-потоці WinUI: {Message}", e.Message);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        RegisterDeepLinkScheme();
        ReadDeepLinkFromActivation();
        base.OnLaunched(args);
    }

    // Реєстрація схеми railwayerp:// для поточного користувача (аналог Intent Filter на Android)
    private static void RegisterDeepLinkScheme()
    {
        var exe = Environment.ProcessPath!;
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{DeepLinkService.Scheme}");
        key.SetValue(string.Empty, $"URL:{DeepLinkService.Scheme}");
        key.SetValue("URL Protocol", string.Empty);
        using var icon = key.CreateSubKey("DefaultIcon");
        icon.SetValue(string.Empty, $"\"{exe}\",0");
        using var command = key.CreateSubKey(@"shell\open\command");
        command.SetValue(string.Empty, $"\"{exe}\" \"%1\"");
    }

    private static void ReadDeepLinkFromActivation()
    {
        var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (activation.Kind == ExtendedActivationKind.Protocol
            && activation.Data is IProtocolActivatedEventArgs protocol)
        {
            DeepLinkService.PendingUri = protocol.Uri;
            return;
        }

        // запуск із командного рядка: RailwayErp.MvvmDemo.exe railwayerp://train/3
        var arg = Environment.GetCommandLineArgs()
            .FirstOrDefault(a => a.StartsWith(DeepLinkService.Scheme + "://", StringComparison.OrdinalIgnoreCase));
        if (arg is not null && Uri.TryCreate(arg, UriKind.Absolute, out var uri))
            DeepLinkService.PendingUri = uri;
    }
}
