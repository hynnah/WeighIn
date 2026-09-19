using Android.App;
using Android.Content;

namespace WeighIn.Platforms.Android;

public static class ReminderScheduler
{
    private const int RequestCode = 5001;
    public const string HourExtra = "reminder_hour";
    public const string MinuteExtra = "reminder_minute";

    public static void Schedule(TimeSpan time)
    {
        var context = global::Android.App.Application.Context;
        if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager)
            return;

        var intent = BuildIntent(context, time);
        var pendingIntent = PendingIntent.GetBroadcast(context, RequestCode, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;

        var triggerAtMillis = NextTriggerTimeMillis(time);
        alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
    }

    public static void Cancel()
    {
        var context = global::Android.App.Application.Context;
        if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager)
            return;

        var intent = new Intent(context, typeof(ReminderBroadcastReceiver));
        var pendingIntent = PendingIntent.GetBroadcast(context, RequestCode, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
        alarmManager.Cancel(pendingIntent);
    }

    private static Intent BuildIntent(Context context, TimeSpan time)
    {
        var intent = new Intent(context, typeof(ReminderBroadcastReceiver));
        intent.PutExtra(HourExtra, time.Hours);
        intent.PutExtra(MinuteExtra, time.Minutes);
        return intent;
    }

    internal static long NextTriggerTimeMillis(TimeSpan time)
    {
        var now = DateTime.Now;
        var next = now.Date + time;
        if (next <= now)
            next = next.AddDays(1);

        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(next.ToUniversalTime() - epoch).TotalMilliseconds;
    }
}
