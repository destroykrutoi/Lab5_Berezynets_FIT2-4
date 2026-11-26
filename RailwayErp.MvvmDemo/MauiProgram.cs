using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Data;
using RailwayErp.MvvmDemo.Services;
using RailwayErp.MvvmDemo.ViewModels;
using RailwayErp.MvvmDemo.Views;
using Serilog;

namespace RailwayErp.MvvmDemo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // журнал і глобальні обробники — до створення застосунку, щоб не пропустити ранні помилки
        Log.Logger = LoggingSetup.CreateLogger();
        LoggingSetup.RegisterGlobalExceptionHandlers();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);
#if DEBUG
        builder.Logging.AddDebug();
#endif

        // сервіс автоматичного збору крашів (Sentry)
        if (SentryConfig.IsConfigured)
        {
            builder.UseSentry(options =>
            {
                options.Dsn = SentryConfig.Dsn;
                options.Release = SentryConfig.Release;
#if DEBUG
                options.Environment = "debug";
#else
                options.Environment = "production";
#endif
                options.SendDefaultPii = false;           // без IP-адреси та імені користувача ОС
                options.ServerName = "railwayerp-client"; // замість імені комп'ютера
                options.AttachStacktrace = true;
                options.MinimumBreadcrumbLevel = LogLevel.Information;
                options.MinimumEventLevel = LogLevel.Error;
                options.IncludeTextInBreadcrumbs = false; // тексти полів введення не надсилаються
            });
        }

        // стан застосунку, тема, deep link (з лабораторної роботи № 3)
        builder.Services.AddSingleton<IAppStateService, AppStateService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<IDeepLinkService, DeepLinkService>();

        // локальна БД, репозиторій, шифрування, черга та синхронізація (з лабораторної роботи № 4)
        builder.Services.AddSingleton<AppDatabase>();
        builder.Services.AddSingleton<ICryptoService, AesCryptoService>();
        builder.Services.AddSingleton<ISyncQueue, SyncQueue>();
        builder.Services.AddSingleton<INetworkService, NetworkService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();
        builder.Services.AddSingleton<ITrainRouteRepository, TrainRouteRepository>();

        // журнал, аналітика, звіти про помилки
        builder.Services.AddSingleton<ILogReaderService, LogReaderService>();
        builder.Services.AddSingleton<IDiagnosticsService, DiagnosticsService>();

        builder.Services.AddTransient<TrainRouteListViewModel>();
        builder.Services.AddTransient<RouteEditViewModel>();
        builder.Services.AddTransient<DatabaseViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<LogViewModel>();

        builder.Services.AddTransient<TrainRouteListPage>();
        builder.Services.AddTransient<RouteEditPage>();
        builder.Services.AddTransient<DatabasePage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<LogPage>();

        var app = builder.Build();
        Log.ForContext("SourceContext", "MauiProgram").Information(
            "Застосунок запущено: версія {Version}, {Platform} {OsVersion}, збір крашів: {CrashReporting}",
            AppInfo.VersionString, DeviceInfo.Platform, DeviceInfo.VersionString,
            SentryConfig.IsConfigured ? "Sentry" : "вимкнено");
        return app;
    }
}
