using RailwayErp.MvvmDemo.Views;

namespace RailwayErp.MvvmDemo;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("routeedit", typeof(RouteEditPage));
    }
}
