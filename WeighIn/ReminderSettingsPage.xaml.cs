using WeighIn.Services;

namespace WeighIn;

public partial class ReminderSettingsPage : ContentPage
{
    private readonly AppDatabase database = new();

    public ReminderSettingsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var profile = await database.GetProfileAsync();
        ReminderSwitch.IsToggled = profile.RemindersEnabled;
        ReminderTimePicker.Time = TimeSpan.TryParse(profile.ReminderTime, out var time) ? time : new TimeSpan(7, 0, 0);
        RefreshTimeDisplayLabel();
        RefreshTimeRowState();
    }

    private void OnReminderToggled(object? sender, ToggledEventArgs e) => RefreshTimeRowState();

    private void OnReminderTimeSelected(object? sender, TimeChangedEventArgs e) => RefreshTimeDisplayLabel();

    private void RefreshTimeDisplayLabel()
    {
        var time = ReminderTimePicker.Time.GetValueOrDefault();
        ReminderTimeDisplayLabel.Text = DateTime.Today.Add(time).ToString("hh:mm tt");
    }

    private void RefreshTimeRowState()
    {
        TimeRow.Opacity = ReminderSwitch.IsToggled ? 1.0 : 0.5;
        TimeRow.InputTransparent = !ReminderSwitch.IsToggled;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var profile = await database.GetProfileAsync();
        profile.RemindersEnabled = ReminderSwitch.IsToggled;
        profile.ReminderTime = ReminderTimePicker.Time.GetValueOrDefault().ToString(@"hh\:mm");

        await database.SaveProfileAsync(profile);
        await ReminderScheduleHelper.ApplyAsync(profile);

        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
