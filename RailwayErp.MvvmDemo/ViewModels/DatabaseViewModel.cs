using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RailwayErp.MvvmDemo.Data;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.ViewModels;

// Перегляд вмісту БД «як є»: видно, що чутливе поле зберігається зашифрованим
public partial class DatabaseViewModel : ObservableObject
{
    private readonly AppDatabase _database;

    public DatabaseViewModel(AppDatabase database) => _database = database;

    public ObservableCollection<string> RouteRows { get; } = new();
    public ObservableCollection<string> QueueRows { get; } = new();

    [ObservableProperty] private string _summary = string.Empty;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var db = await _database.GetConnectionAsync();
        var version = await _database.GetVersionAsync();

        RouteRows.Clear();
        foreach (var r in await db.QueryAsync<TrainRoute>("SELECT * FROM TrainRoute ORDER BY Id"))
            RouteRows.Add($"Id={r.Id} | {r.Name} | {r.Origin} — {r.Destination} | {r.DepartureTime:HH:mm}\nChiefDocumentNumber = {r.ChiefDocumentNumber}");

        QueueRows.Clear();
        foreach (var q in await db.QueryAsync<SyncQueueItem>("SELECT * FROM SyncQueue ORDER BY Id"))
            QueueRows.Add($"#{q.Id} {q.Operation} TrainRoute Id={q.EntityId} ({q.CreatedAt:HH:mm:ss}), спроб: {q.Attempts}");

        Summary = $"Файл: railway.db3, версія схеми (user_version): {version}\n" +
                  $"TrainRoute: {RouteRows.Count} записів, SyncQueue: {QueueRows.Count} записів";
    }
}
