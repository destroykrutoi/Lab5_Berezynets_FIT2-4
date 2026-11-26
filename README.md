# Лаб5

Логування Serilog (консоль + файл з щоденною ротацією), глобальна обробка винятків, екран перегляду логів, збір крашів Sentry.

Запуск (Windows 10/11, .NET 8 SDK з робочим навантаженням maui-windows):

```
cd RailwayErp.MvvmDemo
dotnet build -c Debug
dotnet run -f net8.0-windows10.0.19041.0
```

Спочатку запустити заглушковий сервер синхронізації:

```
dotnet run --project RailwayErp.StubApi
```
Сервер слухає http://localhost:5085, застосунок відправляє туди чергу змін.

Журнал: %LOCALAPPDATA%\User Name\ua.knteu.railwayerp.lab5\Data\logs\app-yyyyMMdd.log (зберігаються 7 останніх файлів).

Збір крашів Sentry вмикається DSN проєкту у змінній середовища SENTRY_DSN (або константі DefaultDsn у Services/SentryConfig.cs):

```
$env:SENTRY_DSN = "https://<key>@<org>.ingest.sentry.io/<project>"
dotnet run -f net8.0-windows10.0.19041.0
```

Моделювання помилок — вкладка «Налаштування», розділ «Діагностика».
