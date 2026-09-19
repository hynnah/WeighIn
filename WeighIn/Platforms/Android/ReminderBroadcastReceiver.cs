using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace WeighIn.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false, Label = "WeighIn daily reminder")]
public sealed class ReminderBroadcastReceiver : BroadcastReceiver
{
    private const string ChannelId = "weighin_daily_reminder";
    private const int NotificationId = 5002;

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null)
            return;

        ShowNotification(context);

        var hour = intent?.GetIntExtra(ReminderScheduler.HourExtra, 7) ?? 7;
        var minute = intent?.GetIntExtra(ReminderScheduler.MinuteExtra, 0) ?? 0;
        ReminderScheduler.Schedule(new TimeSpan(hour, minute, 0));
    }

    private static void ShowNotification(Context context)
    {
        EnsureChannel(context);

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? string.Empty);
        var contentIntent = PendingIntent.GetActivity(context, 0, launchIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle("Time to weigh in")
            .SetContentText("Log today's weight for the most consistent trend.")
            .SetSmallIcon(context.ApplicationInfo?.Icon ?? global::Android.Resource.Drawable.IcMenuInfoDetails)
            .SetAutoCancel(true)
            .SetContentIntent(contentIntent)
            .SetPriority(NotificationCompat.PriorityDefault)
            .Build();

        NotificationManagerCompat.From(context).Notify(NotificationId, notification);
    }

    private static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        if (context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
            return;

        var channel = new NotificationChannel(ChannelId, "Daily weigh-in reminder", NotificationImportance.Default);
        manager.CreateNotificationChannel(channel);
    }
}
