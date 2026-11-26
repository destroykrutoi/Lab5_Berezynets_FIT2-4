using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Data;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services;

public interface ITrainRouteRepository
{
    Task<List<TrainRoute>> GetAllAsync();
    Task<TrainRoute?> GetByIdAsync(int id);
    Task<int> SaveAsync(TrainRoute route);
    Task<int> DeleteAsync(TrainRoute route);
}

// Репозиторій: ViewModel не працює з SQLiteAsyncConnection напряму
public class TrainRouteRepository : ITrainRouteRepository
{
    private readonly AppDatabase _database;
    private readonly ICryptoService _crypto;
    private readonly ISyncQueue _queue;
    private readonly ILogger<TrainRouteRepository> _logger;

    public TrainRouteRepository(AppDatabase database, ICryptoService crypto, ISyncQueue queue,
        ILogger<TrainRouteRepository> logger)
    {
        _database = database;
        _crypto = crypto;
        _queue = queue;
        _logger = logger;
    }

    public async Task<List<TrainRoute>> GetAllAsync()
    {
        var watch = Stopwatch.StartNew();
        try
        {
            var db = await _database.GetConnectionAsync();
            var list = await db.Table<TrainRoute>().OrderBy(r => r.DepartureTime).ToListAsync();
            foreach (var r in list)
                r.ChiefDocumentNumber = await _crypto.DecryptAsync(r.ChiefDocumentNumber);
            _logger.LogDebug("Прочитано {Count} рядків TrainRoute за {Elapsed} мс", list.Count, watch.ElapsedMilliseconds);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка читання маршрутів із локальної БД");
            throw;
        }
    }

    public async Task<TrainRoute?> GetByIdAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        var route = await db.FindAsync<TrainRoute>(id);
        if (route is null)
        {
            _logger.LogWarning("Маршрут з Id = {RouteId} не знайдено в БД", id);
            return null;
        }
        route.ChiefDocumentNumber = await _crypto.DecryptAsync(route.ChiefDocumentNumber);
        return route;
    }

    public async Task<int> SaveAsync(TrainRoute route)
    {
        var isNew = route.Id == 0;
        try
        {
            var db = await _database.GetConnectionAsync();

            // запис у БД виконується з копії, де чутливе поле зашифроване
            var stored = Clone(route);
            stored.UpdatedAt = DateTime.UtcNow;
            stored.ChiefDocumentNumber = await _crypto.EncryptAsync(route.ChiefDocumentNumber);

            var result = isNew ? await db.InsertAsync(stored) : await db.UpdateAsync(stored);
            route.Id = stored.Id;
            route.UpdatedAt = stored.UpdatedAt;

            await _queue.EnqueueAsync(isNew ? "Insert" : "Update", stored);
            // номер документа (чутливі дані) у журнал не потрапляє
            _logger.LogInformation("Маршрут Id = {RouteId} «{RouteName}» збережено ({Operation})",
                route.Id, route.Name, isNew ? "Insert" : "Update");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не вдалося зберегти маршрут «{RouteName}» ({Operation})",
                route.Name, isNew ? "Insert" : "Update");
            throw;
        }
    }

    public async Task<int> DeleteAsync(TrainRoute route)
    {
        try
        {
            var db = await _database.GetConnectionAsync();
            var result = await db.DeleteAsync<TrainRoute>(route.Id);
            await _queue.EnqueueAsync("Delete", new TrainRoute { Id = route.Id, UpdatedAt = DateTime.UtcNow });
            _logger.LogInformation("Маршрут Id = {RouteId} «{RouteName}» видалено", route.Id, route.Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не вдалося видалити маршрут Id = {RouteId}", route.Id);
            throw;
        }
    }

    private static TrainRoute Clone(TrainRoute r) =>
        JsonSerializer.Deserialize<TrainRoute>(JsonSerializer.Serialize(r))!;
}
