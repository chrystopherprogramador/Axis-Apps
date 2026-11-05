namespace Axis.App.Vendedor;

public class AppShell : Shell
{
    public AppShell()
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Shell.SetNavBarIsVisible(this, false);

        Items.Add(new ShellContent
        {
            Route = "LoginPage",
            ContentTemplate = new DataTemplate(typeof(Pages.LoginPage))
        });

        Items.Add(new ShellContent
        {
            Route = "HomePage",
            ContentTemplate = new DataTemplate(typeof(Pages.HomePage))
        });

        Routing.RegisterRoute("ConfigPage", typeof(Pages.ConfigPage));
        Routing.RegisterRoute("PreVendaPage", typeof(Pages.PreVendaPage));
        Routing.RegisterRoute("MinhasPreVendasPage", typeof(Pages.MinhasPreVendasPage));
        Routing.RegisterRoute("BuscarProdutoPage", typeof(Pages.BuscarProdutoPage));
        Routing.RegisterRoute("BuscarClientePage", typeof(Pages.BuscarClientePage));
        Routing.RegisterRoute("MetasPage", typeof(Pages.MetasPage));
        Routing.RegisterRoute("DashboardPage", typeof(Pages.DashboardPage));
    }
}
