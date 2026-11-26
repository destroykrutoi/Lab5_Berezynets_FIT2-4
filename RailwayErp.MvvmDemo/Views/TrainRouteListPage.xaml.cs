using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Services;
using RailwayErp.MvvmDemo.ViewModels;

namespace RailwayErp.MvvmDemo.Views;

public partial class TrainRouteListPage : ContentPage
{
    private readonly IAppStateService _state;

    public TrainRouteListPage(TrainRouteListViewModel viewModel, IAppStateService state)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _state = state;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _state.Log("TrainRouteListPage.OnAppearing → LoadCommand", LogLevel.Debug);
        (BindingContext as TrainRouteListViewModel)?.LoadCommand.Execute(null);
    }
}
