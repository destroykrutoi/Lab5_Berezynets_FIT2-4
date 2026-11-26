namespace RailwayErp.MvvmDemo.Services;

// Параметри сервісу збору крашів Sentry (sentry.io)
public static class SentryConfig
{
    // DSN проєкту в Sentry; можна перевизначити змінною середовища SENTRY_DSN
    private const string DefaultDsn = "";

    public static string Dsn =>
        Environment.GetEnvironmentVariable("SENTRY_DSN") is { Length: > 0 } env ? env : DefaultDsn;

    public static bool IsConfigured => Dsn.Length > 0;

    public const string Release = "railwayerp-mvvmdemo@1.0.0+lab5";
}
