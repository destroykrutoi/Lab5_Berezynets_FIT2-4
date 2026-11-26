using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo.ViewModels;

// Додавання та редагування маршруту
public partial class RouteEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly ITrainRouteRepository _repository;
    private readonly ISyncService _sync;
    private readonly INetworkService _network;
    private readonly IDiagnosticsService _diagnostics;
    private readonly ILogger<RouteEditViewModel> _logger;
    private TrainRoute _route = new();

    public RouteEditViewModel(ITrainRouteRepository repository, ISyncService sync, INetworkService network,
        IDiagnosticsService diagnostics, ILogger<RouteEditViewModel> logger)
    {
        _repository = repository;
        _sync = sync;
        _network = network;
        _diagnostics = diagnostics;
        _logger = logger;
    }

    [ObservableProperty] private string _header = "Новий маршрут";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _origin = string.Empty;
    [ObservableProperty] private string _destination = string.Empty;
    [ObservableProperty] private string _departure = string.Empty;
    [ObservableProperty] private string _chiefDocumentNumber = string.Empty;
    [ObservableProperty] private string _error = string.Empty;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && int.TryParse(value?.ToString(), out var id))
        {
            try
            {
                var route = await _repository.GetByIdAsync(id);
                if (route is null)
                {
                    Error = $"Маршрут з Id = {id} не знайдено";
                    return;
                }
                _route = route;
                Header = $"Редагування маршруту (Id = {id})";
                Name = route.Name;
                Origin = route.Origin;
                Destination = route.Destination;
                Departure = route.DepartureTime.ToString("HH:mm");
                ChiefDocumentNumber = route.ChiefDocumentNumber ?? string.Empty;
                _diagnostics.TrackEvent("RouteOpened", new Dictionary<string, string> { ["RouteId"] = id.ToString() });
            }
            catch (Exception ex)
            {
                // async void: виняток без catch став би необробленим
                _logger.LogError(ex, "Помилка відкриття маршруту Id = {RouteId}", id);
                Error = "Не вдалося відкрити маршрут — деталі в журналі";
            }
        }
        else
        {
            _logger.LogDebug("Відкрито форму нового маршруту");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) || !TimeSpan.TryParse(Departure, out var time))
        {
            _logger.LogWarning("Невдале збереження маршруту: некоректні дані (назва порожня: {NameEmpty}, час: «{Departure}»)",
                string.IsNullOrWhiteSpace(Name), Departure);
            Error = "Вкажіть назву поїзда та час відправлення у форматі ГГ:ХХ";
            return;
        }

        _route.Name = Name.Trim();
        _route.Origin = Origin.Trim();
        _route.Destination = Destination.Trim();
        _route.DepartureTime = DateTime.Today.Add(time);
        _route.ChiefDocumentNumber = ChiefDocumentNumber.Trim();

        try
        {
            await _repository.SaveAsync(_route);
            _diagnostics.TrackEvent("RouteSaved", new Dictionary<string, string> { ["RouteId"] = _route.Id.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка збереження маршруту «{RouteName}»", _route.Name);
            Error = "Не вдалося зберегти маршрут — деталі в журналі";
            return;
        }

        if (_network.IsOnline)
            await _sync.SyncAsync();

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
