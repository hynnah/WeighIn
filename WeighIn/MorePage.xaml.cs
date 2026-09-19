using WeighIn.Controls;
using WeighIn.Services;

namespace WeighIn;

public partial class MorePage : ContentPage
{
    private readonly AppDatabase database = new();
    private readonly NavIconDrawable homeIcon = new() { Kind = NavIconKind.Home };
    private readonly NavIconDrawable trendsIcon = new() { Kind = NavIconKind.Trends };
    private readonly NavIconDrawable addIcon = new() { Kind = NavIconKind.Add, Color = Colors.White };
    private readonly NavIconDrawable calendarIcon = new() { Kind = NavIconKind.Calendar };
    private readonly NavIconDrawable moreIcon = new() { Kind = NavIconKind.More };

    public MorePage()
    {
        InitializeComponent();
        HomeIcon.Drawable = homeIcon;
        TrendsIcon.Drawable = trendsIcon;
        AddIcon.Drawable = addIcon;
        CalendarIcon.Drawable = calendarIcon;
        MoreIcon.Drawable = moreIcon;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        DarkModeSwitch.IsToggled = isDark;

        var profile = await database.GetProfileAsync();
        var unit = profile.WeightUnitPreference == "lb" ? "lb" : "kg";
        var standardLabel = profile.BmiStandard == "General" ? "WHO standard" : "Asian standard";
        var heightLabel = unit == "lb"
            ? $"{Math.Round(profile.HeightCm / 2.54)} in"
            : $"{profile.HeightCm:0} cm";
        ProfileSummaryLabel.Text = $"{heightLabel} · {unit} · {standardLabel}";

        if (profile.TargetWeightKg is { } goalKg && profile.TargetDate is { } goalDate)
        {
            var goalDisplay = unit == "lb" ? goalKg / 0.45359237 : goalKg;
            GoalSummaryLabel.Text = $"{goalDisplay:0.0} {unit} by {goalDate:d MMM}";
        }
        else
        {
            GoalSummaryLabel.Text = "Not set";
        }

        ReminderTagLabel.Text = profile is { RemindersEnabled: true, ReminderTime: { } time } ? time : "Off";
        LockTagLabel.Text = profile.LockEnabled ? "On" : "Off";
        LockNowLabel.TextColor = profile.LockEnabled ? Color.FromArgb("#9184D9") : Color.FromArgb("#8D8A82");

        var muted = NavBarColors.Muted(isDark);
        homeIcon.Color = muted;
        trendsIcon.Color = muted;
        calendarIcon.Color = muted;
        moreIcon.Color = NavBarColors.Active;
        HomeLabel.TextColor = muted;
        TrendsLabel.TextColor = muted;
        CalendarLabel.TextColor = muted;
        MoreLabel.TextColor = NavBarColors.Active;
        HomeIcon.Invalidate();
        TrendsIcon.Invalidate();
        CalendarIcon.Invalidate();
        MoreIcon.Invalidate();
    }

    private async void OnHistoryTapped(object? sender, EventArgs e) => await Navigation.PushModalAsync(new HistoryPage());
    private async void OnGoalTapped(object? sender, EventArgs e) => await Navigation.PushModalAsync(new GoalPage());
    private async void OnSettingsTapped(object? sender, EventArgs e) => await Navigation.PushModalAsync(new SettingsPage());

    private static readonly FilePickerFileType CsvFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values", "text/plain", "application/csv" } },
        { DevicePlatform.iOS, new[] { "public.comma-separated-values-text" } },
        { DevicePlatform.MacCatalyst, new[] { "public.comma-separated-values-text" } },
        { DevicePlatform.WinUI, new[] { ".csv" } }
    });

    private async void OnExportImportTapped(object? sender, EventArgs e)
    {
        var action = await DisplayActionSheetAsync("Export / import CSV", "Cancel", null, "Export CSV", "Import CSV");
        if (action == "Export CSV")
            await ExportCsvAsync();
        else if (action == "Import CSV")
            await ImportCsvAsync();
    }

    private async Task ExportCsvAsync()
    {
        var entries = await database.GetEntriesAsync();
        if (entries.Count == 0)
        {
            await DisplayAlertAsync("Nothing to export", "You don't have any weigh-ins logged yet.", "OK");
            return;
        }

        var csv = CsvTransfer.Export(entries);
        var fileName = $"weighin-export-{DateTime.Now:yyyyMMdd-HHmm}.csv";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllTextAsync(filePath, csv);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Export weigh-ins",
            File = new ShareFile(filePath)
        });
    }

    private async Task ImportCsvAsync()
    {
        FileResult? result;
        try
        {
            result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a CSV file",
                FileTypes = CsvFileType
            });
        }
        catch (Exception)
        {
            result = null;
        }

        if (result is null)
            return;

        string csvText;
        using (var stream = await result.OpenReadAsync())
        using (var reader = new StreamReader(stream))
        {
            csvText = await reader.ReadToEndAsync();
        }

        var profile = await database.GetProfileAsync();
        var importResult = CsvTransfer.Import(csvText, profile.HeightCm);

        foreach (var entry in importResult.Entries)
            await database.SaveEntryAsync(entry);

        var message = importResult.SkippedCount > 0
            ? $"Imported {importResult.Entries.Count} weigh-ins. {importResult.SkippedCount} row(s) were skipped (invalid data)."
            : $"Imported {importResult.Entries.Count} weigh-ins.";
        await DisplayAlertAsync("Import complete", message, "OK");
    }

    private async void OnReplaySetupTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//onboard");

    private async void OnLockNowTapped(object? sender, EventArgs e)
    {
        var profile = await database.GetProfileAsync();
        if (!profile.LockEnabled || string.IsNullOrEmpty(profile.Pin))
        {
            await DisplayAlertAsync("No PIN set", "Turn on app lock first by replaying setup.", "OK");
            return;
        }

        await Shell.Current.GoToAsync("//lock");
    }

    private void OnDarkModeToggled(object? sender, ToggledEventArgs e)
    {
        if (Application.Current is null)
            return;

        Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    private async void OnHomeClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/home");
    private async void OnTrendsClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/trends");
    private async void OnAddClicked(object? sender, TappedEventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());
    private async void OnCalendarClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/calendar");
    private async void OnMoreClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
