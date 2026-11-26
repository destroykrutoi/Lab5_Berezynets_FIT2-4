using RailwayErp.MvvmDemo.ViewModels;

namespace RailwayErp.MvvmDemo.Views;

public partial class DatabasePage : ContentPage
{
    public DatabasePage(DatabaseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as DatabaseViewModel)?.LoadCommand.Execute(null);
    }
}
