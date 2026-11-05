using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Axis.App.Vendedor
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            try
            {
                MainApplication.GravarLog("MainActivity.OnCreate INICIO");
                base.OnCreate(savedInstanceState);
                MainApplication.GravarLog("MainActivity.OnCreate OK");
            }
            catch (Exception ex)
            {
                MainApplication.GravarCrash("MainActivity.OnCreate", ex.ToString());
                // Mostrar erro na tela como toast
                Android.Widget.Toast.MakeText(this, "CRASH: " + ex.Message, Android.Widget.ToastLength.Long)?.Show();
            }
        }
    }
}
