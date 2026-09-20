using WeighIn.Models;

namespace WeighIn.Services;

public static class ReminderScheduleHelper
{
    public static async Task ApplyAsync(Profile profile)
    {
#if ANDROID
        if (profile.RemindersEnabled && TimeSpan.TryParse(profile.ReminderTime, out var reminderTime))
        {
            var status = await Permissions.RequestAsync<Platforms.Android.PostNotificationsPermission>();
            if (status == PermissionStatus.Granted)
                Platforms.Android.ReminderScheduler.Schedule(reminderTime);
            else
                Platforms.Android.ReminderScheduler.Cancel();
        }
        else
        {
            Platforms.Android.ReminderScheduler.Cancel();
        }
#else
        await Task.CompletedTask;
#endif
    }
}
