namespace RailwayErp.MvvmDemo.Services;

public interface INetworkService
{
    bool IsOnline { get; }
    bool SimulateOffline { get; set; }
    event EventHandler<bool>? StatusChanged;
}

// Стан мережі: реальний Connectivity + перемикач для імітації офлайн-режиму
public class NetworkService : INetworkService
{
    private readonly IAppStateService _state;
    private bool _simulateOffline;
    private bool _lastOnline;

    public NetworkService(IAppStateService state)
    {
        _state = state;
        _lastOnline = IsOnline;
        Connectivity.Current.ConnectivityChanged += (_, _) => Raise();
    }

    public bool IsOnline =>
        !_simulateOffline && Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public bool SimulateOffline
    {
        get => _simulateOffline;
        set
        {
            if (_simulateOffline == value) return;
            _simulateOffline = value;
            Raise();
        }
    }

    public event EventHandler<bool>? StatusChanged;

    private void Raise()
    {
        var online = IsOnline;
        if (online == _lastOnline) return;
        _lastOnline = online;
        _state.Log(online ? "Мережа: з'єднання з'явилося" : "Мережа: офлайн");
        StatusChanged?.Invoke(this, online);
    }
}
