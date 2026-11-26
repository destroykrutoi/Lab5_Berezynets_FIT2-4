using Serilog;
using Serilog.Events;

namespace RailwayErp.MvvmDemo.Services;

// Налаштування Serilog і глобальних обробників необроблених винятків
public static class LoggingSetup
{
    public const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static string LogDirectory => Path.Combine(FileSystem.AppDataDirectory, "logs");

    public static Serilog.ILogger CreateLogger()
    {
        // кирилиця в консольному sink'у
        // (у віконного застосунку консолі немає, тому stdout перевідкривається в UTF-8 напряму)
        if (Console.IsOutputRedirected)
            Console.SetOut(TextWriter.Synchronized(
                new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false)) { AutoFlush = true }));
        return Configure();
    }

    private static Serilog.ILogger Configure() => new LoggerConfiguration()
#if DEBUG
        .MinimumLevel.Debug()
#else
        .MinimumLevel.Information()
#endif
        // службові повідомлення фреймворку — лише від Warning
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        // sink 1: консоль (для налагодження)
        .WriteTo.Console(outputTemplate: OutputTemplate)
        // sink 2: файл з щоденною ротацією, зберігаються останні 7 файлів
        .WriteTo.File(
            Path.Combine(LogDirectory, "app-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: OutputTemplate)
        .CreateLogger();

    public static void RegisterGlobalExceptionHandlers()
    {
        var log = Log.ForContext("SourceContext", "GlobalExceptionHandler");

        // виняток поза try/catch у будь-якому потоці: застосунок буде завершено
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            log.Fatal(ex, "Необроблений виняток у AppDomain (IsTerminating = {IsTerminating})", e.IsTerminating);
            if (e.IsTerminating)
                Log.CloseAndFlush(); // дописати буфер у файл до завершення процесу
        };

        // виняток у Task, результат якої ніхто не очікував (await/Wait/Result)
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            log.Error(e.Exception, "Необроблений виняток у Task (UnobservedTaskException)");
            e.SetObserved(); // позначити як оброблений, щоб процес не завершився
        };
    }
}
