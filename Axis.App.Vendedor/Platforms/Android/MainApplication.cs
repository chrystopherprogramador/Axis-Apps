using Android.App;
using Android.Runtime;

namespace Axis.App.Vendedor
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            // Capturar TODAS as exceções não tratadas no Android
            AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
            {
                GravarCrash("AndroidEnvironment", args.Exception?.ToString() ?? "null");
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                GravarCrash("AppDomain", (args.ExceptionObject as Exception)?.ToString() ?? "null");
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                GravarCrash("TaskScheduler", args.Exception?.ToString() ?? "null");
                args.SetObserved();
            };
        }

        protected override MauiApp CreateMauiApp()
        {
            try
            {
                GravarLog("CreateMauiApp INICIO");
                var app = MauiProgram.CreateMauiApp();
                GravarLog("CreateMauiApp OK");
                return app;
            }
            catch (Exception ex)
            {
                GravarCrash("CreateMauiApp", ex.ToString());
                throw;
            }
        }

        public static void GravarLog(string msg)
        {
            try
            {
                var path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "axis_log.txt");
                System.IO.File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
                Android.Util.Log.Info("AXIS", msg);
            }
            catch { }
        }

        public static void GravarCrash(string origem, string erro)
        {
            try
            {
                var path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "axis_crash.txt");
                System.IO.File.AppendAllText(path, $"\n=== CRASH [{DateTime.Now}] [{origem}] ===\n{erro}\n");
                Android.Util.Log.Error("AXIS_CRASH", $"[{origem}] {erro}");
            }
            catch { }
        }
    }
}
