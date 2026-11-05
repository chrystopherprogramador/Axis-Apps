using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace Axis.App.Salao.Platforms.Android;

/// <summary>
/// Emite notificações locais no Android usando o NotificationManager.
/// </summary>
public static class NotificacaoAndroid
{
    private const string CanalId = "axis_salao_agendamentos";
    private const string CanalNome = "Agendamentos";
    private static int _idSeq = 1000;

    public static void Mostrar(string titulo, string mensagem)
    {
        try
        {
            var contexto = global::Android.App.Application.Context;
            CriarCanal(contexto);

            var intent = contexto.PackageManager?.GetLaunchIntentForPackage(contexto.PackageName!);
            intent?.AddFlags(ActivityFlags.SingleTop);
            var flags = Build.VERSION.SdkInt >= BuildVersionCodes.S
                ? PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
                : PendingIntentFlags.UpdateCurrent;
            var pending = intent != null
                ? PendingIntent.GetActivity(contexto, 0, intent, flags)
                : null;

            var builder = new NotificationCompat.Builder(contexto, CanalId)
                .SetContentTitle(titulo)
                .SetContentText(mensagem)
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true)
                .SetPriority((int)NotificationPriority.High);

            if (pending != null)
                builder.SetContentIntent(pending);

            var manager = NotificationManagerCompat.From(contexto);
            manager.Notify(_idSeq++, builder.Build());
        }
        catch
        {
            // ignora falhas de notificação (ex.: permissão negada)
        }
    }

    private static void CriarCanal(Context contexto)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var manager = (NotificationManager?)contexto.GetSystemService(Context.NotificationService);
        if (manager == null) return;
        if (manager.GetNotificationChannel(CanalId) != null) return;

        var canal = new NotificationChannel(CanalId, CanalNome, NotificationImportance.High)
        {
            Description = "Novos agendamentos e encaixes"
        };
        manager.CreateNotificationChannel(canal);
    }
}
