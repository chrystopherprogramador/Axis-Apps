using Axis.App.RfidStation.Forms;
using Axis.App.RfidStation.Services;

namespace Axis.App.RfidStation;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        AppConfig.Carregar();
        Application.Run(new LoginForm());
    }
}
