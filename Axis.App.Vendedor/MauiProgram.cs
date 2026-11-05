using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace Axis.App.Vendedor
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Log("MauiProgram.CreateMauiApp INICIO");

            var builder = MauiApp.CreateBuilder();
            Log("Builder criado");

            builder.UseMauiApp<App>();
            Log("UseMauiApp OK");

            builder.UseBarcodeReader();
            Log("UseBarcodeReader OK");

            builder.ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
            Log("ConfigureFonts OK");

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            Log("Build OK");
            return app;
        }

        private static void Log(string msg)
        {
#if ANDROID
            MainApplication.GravarLog(msg);
#endif
            System.Diagnostics.Debug.WriteLine("[AXIS] " + msg);
        }
    }
}
