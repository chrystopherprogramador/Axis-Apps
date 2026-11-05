using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Axis.App.Salao
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
                SolicitarPermissaoNotificacao();
                MainApplication.GravarLog("MainActivity.OnCreate OK");
            }
            catch (Exception ex)
            {
                MainApplication.GravarCrash("MainActivity.OnCreate", ex.ToString());
                Android.Widget.Toast.MakeText(this, "CRASH: " + ex.Message, Android.Widget.ToastLength.Long)?.Show();
            }
        }

        private void SolicitarPermissaoNotificacao()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                {
                    if (CheckSelfPermission("android.permission.POST_NOTIFICATIONS") != Permission.Granted)
                        RequestPermissions(new[] { "android.permission.POST_NOTIFICATIONS" }, 101);
                }
            }
            catch { }
        }
    }
}
