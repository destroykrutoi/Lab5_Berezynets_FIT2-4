using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;
using SQLite;

namespace RailwayErp.MvvmDemo.Data;

// Підключення до SQLite та версіонування схеми через PRAGMA user_version
public class AppDatabase
{
    public const int SchemaVersion = 2;
    public static string DbPath => Path.Combine(FileSystem.AppDataDirectory, "railway.db3");

    private readonly SQLiteAsyncConnection _db;
    private readonly IAppStateService _state;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public AppDatabase(IAppStateService state)
    {
        _state = state;
        _db = new SQLiteAsyncConnection(DbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_initialized) return _db;
        await _initLock.WaitAsync();
        try
        {
            if (!_initialized)
            {
                await MigrateAsync();
                _initialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
        return _db;
    }

    public async Task<int> GetVersionAsync() => await _db.ExecuteScalarAsync<int>("PRAGMA user_version");

    private async Task MigrateAsync()
    {
        var version = await GetVersionAsync();

        if (version < 1)
        {
            await _db.CreateTableAsync<TrainRoute>();
            await _db.ExecuteAsync("PRAGMA user_version = 1");
            _state.Log("Міграція БД: v1 — створено таблицю TrainRoute");
        }

        if (version < 2)
        {
            await _db.CreateTableAsync<SyncQueueItem>();
            await _db.ExecuteAsync("PRAGMA user_version = 2");
            _state.Log("Міграція БД: v2 — створено таблицю SyncQueue");
        }
    }
}
