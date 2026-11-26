using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo.ViewModels;

public partial class TrainRouteListViewModel : ObservableObject
{
    private readonly ITrainRouteRepository _repository;
    private readonly ISyncQueue _queue;
    private readonly ISyncService _sync;
    private readonly INetworkService _network;
    private readonly IDiagnosticsService _diagnostics;
    private readonly ILogger<TrainRouteListViewModel> _logger;

    public TrainRouteListViewModel(ITrainRouteRepository repository, ISyncQueue queue,
        ISyncService sync, INetworkService network, IDiagnosticsService diagnostics,
        ILogger<TrainRouteListViewModel> logger)
    {
        _repository = repository;
        _queue = queue;
        _sync = sync;
        _network = network;
        _diagnostics = diagnostics;
        _logger = logger;

        _isOffline = network.SimulateOffline;
        _queue.QueueChanged += async (_, _) => await MainThread.InvokeOnMainThreadAsync(UpdateQueueAsync);
        _network.StatusChanged += (_, _) => MainThread.BeginInvokeOnMainThread(UpdateNetworkText);
        UpdateNetworkText();
    }

    public ObservableCollection<TrainRoute> Routes { get; } = new();

    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private string _networkText = string.Empty;
    [ObservableProperty] private int _queueCount;
    [ObservableProperty] private string _syncMessage = string.Empty;
    [ObservableProperty] private bool _isEmpty;

    partial void OnIsOfflineChanged(bool value)
    {
        _network.SimulateOffline = value;
        UpdateNetworkText();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        _logger.LogInformation("Завантаження маршрутів розпочато");
        try
        {
            Routes.Clear();
            foreach (var route in await _repository.GetAllAsync())
                Routes.Add(route);
            IsEmpty = Routes.Count == 0;
            await UpdateQueueAsync();
            _logger.LogInformation("Завантаження маршрутів завершено: {Count} записів, у черзі синхронізації {Pending}",
                Routes.Count, QueueCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка завантаження маршрутів");
            SyncMessage = "Не вдалося завантажити маршрути — деталі в журналі";
        }
    }

    [RelayCommand]
    private Task AddAsync() => NavigateAsync("routeedit");

    [RelayCommand]
    private Task EditAsync(TrainRoute? route) =>
        route is null ? Task.CompletedTask : NavigateAsync($"routeedit?id={route.Id}");

    private async Task NavigateAsync(string route)
    {
        try
        {
            _logger.LogDebug("Навігація: {Route}", route);
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка навігації на {Route}", route);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(TrainRoute? route)
    {
        if (route is null) return;
        try
        {
            await _repository.DeleteAsync(route);
            Routes.Remove(route);
            IsEmpty = Routes.Count == 0;
            _diagnostics.TrackEvent("RouteDeleted", new Dictionary<string, string> { ["RouteId"] = route.Id.ToString() });
            if (_network.IsOnline)
                SyncMessage = (await _sync.SyncAsync()).Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка видалення маршруту Id = {RouteId}", route.Id);
            SyncMessage = "Не вдалося видалити маршрут — деталі в журналі";
        }
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        _logger.LogInformation("Користувач запустив синхронізацію вручну");
        var result = await _sync.SyncAsync();
        SyncMessage = result.Message;
        await UpdateQueueAsync();
    }

    private async Task UpdateQueueAsync() => QueueCount = (await _queue.GetPendingAsync()).Count;

    private void UpdateNetworkText() =>
        NetworkText = _network.IsOnline ? "Мережа: онлайн" : "Мережа: офлайн";
}
