using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Data;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services;

public interface ISyncQueue
{
    Task EnqueueAsync(string operation, TrainRoute route);
    Task<List<SyncQueueItem>> GetPendingAsync();
    event EventHandler? QueueChanged;
}

public interface ISyncService
{
    Task<SyncResult> SyncAsync();
}

public record SyncResult(int Sent, int Left, string Message);

// Черга офлайн-змін у таблиці SyncQueue
public class SyncQueue : ISyncQueue
{
    private readonly AppDatabase _database;
    private readonly IAppStateService _state;

    public SyncQueue(AppDatabase database, IAppStateService state)
    {
        _database = database;
        _state = state;
    }

    public event EventHandler? QueueChanged;

    public async Task EnqueueAsync(string operation, TrainRoute route)
    {
        var db = await _database.GetConnectionAsync();
        await db.InsertAsync(new SyncQueueItem
        {
            EntityType = nameof(TrainRoute),
            EntityId = route.Id,
            Operation = operation,
            PayloadJson = JsonSerializer.Serialize(route),
            CreatedAt = DateTime.Now
        });
        _state.Log($"SyncQueue: додано {operation} для TrainRoute Id = {route.Id}", LogLevel.Debug);
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<List<SyncQueueItem>> GetPendingAsync()
    {
        var db = await _database.GetConnectionAsync();
        return await db.Table<SyncQueueItem>().OrderBy(i => i.Id).ToListAsync();
    }

    internal async Task RemoveAsync(SyncQueueItem item)
    {
        var db = await _database.GetConnectionAsync();
        await db.DeleteAsync(item);
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    internal async Task MarkFailedAsync(SyncQueueItem item)
    {
        var db = await _database.GetConnectionAsync();
        item.Attempts++;
        await db.UpdateAsync(item);
    }
}

// Сервіс синхронізації: послідовно відправляє чергу на REST API
public class SyncService : ISyncService
{
    public const string ServerUrl = "http://localhost:5085/";

    private readonly SyncQueue _queue;
    private readonly INetworkService _network;
    private readonly ILogger<SyncService> _logger;
    private readonly HttpClient _http = new() { BaseAddress = new Uri(ServerUrl), Timeout = TimeSpan.FromSeconds(5) };
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SyncService(ISyncQueue queue, INetworkService network, ILogger<SyncService> logger)
    {
        _queue = (SyncQueue)queue;
        _network = network;
        _logger = logger;
        // синхронізація запускається автоматично при появі з'єднання
        _network.StatusChanged += async (_, online) => { if (online) await SyncAsync(); };
    }

    public async Task<SyncResult> SyncAsync()
    {
        if (!_network.IsOnline)
        {
            var waiting = (await _queue.GetPendingAsync()).Count;
            _logger.LogWarning("Синхронізацію пропущено: немає з'єднання, у черзі {Pending} змін", waiting);
        }

        await _lock.WaitAsync();
        try
        {
            var sent = 0;
            foreach (var item in await _queue.GetPendingAsync())
            {
                try
                {
                    using var response = await SendAsync(item);
                    // 404 при видаленні вже відсутнього запису вважаємо успіхом
                    if (response.IsSuccessStatusCode || (item.Operation == "Delete" && response.StatusCode == HttpStatusCode.NotFound))
                    {
                        await _queue.RemoveAsync(item);
                        sent++;
                        _logger.LogInformation("Sync: {Operation} Id = {RouteId} → {StatusCode}", item.Operation, item.EntityId, (int)response.StatusCode);
                    }
                    else
                    {
                        await _queue.MarkFailedAsync(item);
                        _logger.LogWarning("Sync: сервер відповів {StatusCode} на {Operation} Id = {RouteId}, синхронізацію зупинено", (int)response.StatusCode, item.Operation, item.EntityId);
                        break;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    await _queue.MarkFailedAsync(item);
                    _logger.LogError(ex, "Sync: сервер {ServerUrl} недоступний, зміни залишаються в черзі (спроба {Attempt})", ServerUrl, item.Attempts);
                    break;
                }
            }

            var left = (await _queue.GetPendingAsync()).Count;
            return new SyncResult(sent, left, $"Відправлено: {sent}, у черзі: {left}");
        }
        finally
        {
            _lock.Release();
        }
    }

    private Task<HttpResponseMessage> SendAsync(SyncQueueItem item)
    {
        var content = new StringContent(item.PayloadJson, Encoding.UTF8, "application/json");
        return item.Operation switch
        {
            "Insert" => _http.PostAsync("api/routes", content),
            "Update" => _http.PutAsync($"api/routes/{item.EntityId}", content),
            "Delete" => _http.DeleteAsync($"api/routes/{item.EntityId}"),
            _ => throw new InvalidOperationException(item.Operation)
        };
    }
}
