namespace Axis.App.Salao;

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
        Routing.RegisterRoute("AgendaPage", typeof(Pages.AgendaPage));
        Routing.RegisterRoute("AgendamentoDetalhePage", typeof(Pages.AgendamentoDetalhePage));
        Routing.RegisterRoute("FinanceiroPage", typeof(Pages.FinanceiroPage));
    }
}
