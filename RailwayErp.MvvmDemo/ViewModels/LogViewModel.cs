using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RailwayErp.MvvmDemo.Services;
using Serilog.Events;

namespace RailwayErp.MvvmDemo.ViewModels;

// Екран перегляду локальних логів: останні N записів із файлу з фільтром за рівнем
public partial class LogViewModel : ObservableObject
{
    private static readonly LogEventLevel[] Levels =
    {
        LogEventLevel.Verbose, LogEventLevel.Debug, LogEventLevel.Information,
        LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal
    };

    private readonly ILogReaderService _reader;
    private readonly IDiagnosticsService _diagnostics;

    public LogViewModel(ILogReaderService reader, IDiagnosticsService diagnostics)
    {
        _reader = reader;
        _diagnostics = diagnostics;
    }

    public IReadOnlyList<string> LevelOptions { get; } = new[]
    {
        "Усі рівні", "Debug і вище", "Information і вище", "Warning і вище", "Error і вище", "Лише Fatal"
    };

    public IReadOnlyList<int> CountOptions { get; } = new[] { 20, 50, 100, 200 };

    public ObservableCollection<LogRecord> Entries { get; } = new();

    [ObservableProperty] private int _selectedLevelIndex;
    [ObservableProperty] private int _selectedCount = 50;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private string _reportStatus = string.Empty;
    [ObservableProperty] private bool _isBusy;

    partial void OnSelectedLevelIndexChanged(int value) => RefreshCommand.Execute(null);
    partial void OnSelectedCountChanged(int value) => RefreshCommand.Execute(null);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var level = Levels[Math.Clamp(SelectedLevelIndex, 0, Levels.Length - 1)];
        var records = await _reader.ReadLatestAsync(SelectedCount, level);

        Entries.Clear();
        foreach (var r in records)
            Entries.Add(r);

        Summary = $"Файл: {_reader.CurrentFileName} · показано {Entries.Count} (рівень ≥ {LogReaderService.ShortName(level)}, " +
                  $"ERR/FTL: {records.Count(r => r.Level >= LogEventLevel.Error)})";
    }

    [RelayCommand]
    private async Task ReportAsync()
    {
        IsBusy = true;
        try
        {
            ReportStatus = await _diagnostics.SendBugReportAsync();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ReportStatus = $"Не вдалося сформувати звіт: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
