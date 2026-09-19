using Android.App;
using Android.Content;
using WeighIn.Models;

namespace WeighIn.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true, Label = "WeighIn boot receiver")]
[IntentFilter([Intent.ActionBootCompleted], Categories = [Intent.CategoryDefault])]
public sealed class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action != Intent.ActionBootCompleted)
            return;

        try
        {
            var dbPath = Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "weighin.db3");
            if (!File.Exists(dbPath))
                return;

            using var connection = new SQLite.SQLiteConnection(dbPath);
            var profile = connection.Table<Profile>().FirstOrDefault();
            if (profile is { RemindersEnabled: true, ReminderTime: { } time } && TimeSpan.TryParse(time, out var reminderTime))
                ReminderScheduler.Schedule(reminderTime);
        }
        catch
        {
            // Best-effort: skip rescheduling if the device just booted into a bad state.
        }
    }
}
