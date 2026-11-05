namespace Axis.App.RFID;

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
        Routing.RegisterRoute("EmpresaSelectPage", typeof(Pages.EmpresaSelectPage));
        Routing.RegisterRoute("InventarioPage", typeof(Pages.InventarioPage));
        Routing.RegisterRoute("LocalizadorPage", typeof(Pages.LocalizadorPage));
        Routing.RegisterRoute("InventarioParcialPage", typeof(Pages.InventarioParcialPage));
        Routing.RegisterRoute("DiagnosticoPage", typeof(Pages.DiagnosticoPage));
    }
}
