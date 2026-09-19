using Microsoft.Maui.Controls.Shapes;
using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn;

public partial class LogSheetPage : ContentPage
{
    private readonly AppDatabase database = new();
    private readonly DateTime forDate;
    private Profile profile = new();
    private List<WeightEntry> dayReadings = new();
    private List<DailySummary> summaries = new();

    public LogSheetPage(DateTime? forDate = null)
    {
        InitializeComponent();
        this.forDate = (forDate ?? DateTime.Today).Date;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        profile = await database.GetProfileAsync();
        var unit = profile.WeightUnitPreference == "lb" ? "lb" : "kg";
        UnitLabel.Text = unit;

        var allEntries = await database.GetEntriesAsync();
        dayReadings = allEntries.Where(entry => entry.DateTime.Date == forDate).OrderBy(entry => entry.DateTime).ToList();
        summaries = DailySummary.FromEntries(allEntries);

        DateLabel.Text = forDate == DateTime.Today ? "Today" : forDate.ToString("ddd, dd MMM yyyy");
        SheetTitleLabel.Text = dayReadings.Count > 0 ? "ADD WEIGH-IN" : "LOG WEIGHT";
        PrimaryActionButton.Text = dayReadings.Count > 0 ? "Add" : "Save";
        DeleteDayButton.IsVisible = dayReadings.Count > 0;

        WeightEntryInput.Text = string.Empty;
        NoteEntry.Text = string.Empty;

        BuildChips(unit);
        BuildQuickAdjustChips(unit);
        UpdateDaySummary(unit);
        UpdateDeltaLabel(unit);
    }

    private void BuildChips(string unit)
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var chipBackground = isDark ? Color.FromArgb("#3A3266") : Color.FromArgb("#DCD5F5");
        var chipText = isDark ? Color.FromArgb("#D8D2FF") : Color.FromArgb("#4A3E8C");
        var removeColor = isDark ? Color.FromArgb("#75798C") : Color.FromArgb("#8D8A82");

        ChipsLayout.Children.Clear();
        foreach (var reading in dayReadings)
        {
            var displayWeight = unit == "lb" ? reading.WeightKg / 0.45359237 : reading.WeightKg;
            var chip = new Border
            {
                BackgroundColor = chipBackground,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                StrokeThickness = 0,
                Padding = new Thickness(10, 5),
                Margin = new Thickness(0, 0, 6, 6),
                MinimumHeightRequest = 26
            };

            var row = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            row.Children.Add(new Label
            {
                Text = $"{displayWeight:0.0} {unit} · {reading.DateTime:HH:mm}",
                FontSize = 11,
                TextColor = chipText,
                VerticalOptions = LayoutOptions.Center
            });

            var removeButton = new Button
            {
                Text = "✕",
                FontSize = 10,
                Padding = 0,
                WidthRequest = 18,
                HeightRequest = 18,
                BackgroundColor = Colors.Transparent,
                TextColor = removeColor,
                CommandParameter = reading.Id
            };
            removeButton.Clicked += OnRemoveReadingClicked;
            row.Children.Add(removeButton);

            chip.Content = row;
            ChipsLayout.Children.Add(chip);
        }
    }

    private void BuildQuickAdjustChips(string unit)
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var buttonBackground = isDark ? Color.FromArgb("#3A3266") : Color.FromArgb("#DCD5F5");
        var buttonText = isDark ? Color.FromArgb("#D8D2FF") : Color.FromArgb("#4A3E8C");

        QuickAdjustLayout.Children.Clear();
        double[] deltas = unit == "lb" ? [-1.0, -0.5, 0.5, 1.0] : [-0.5, -0.1, 0.1, 0.5];
        foreach (var delta in deltas)
        {
            var button = new Button
            {
                Text = delta > 0 ? $"+{delta:0.0}" : delta.ToString("0.0"),
                FontSize = 12,
                Padding = new Thickness(12, 6),
                CornerRadius = 12,
                BackgroundColor = buttonBackground,
                TextColor = buttonText,
                Margin = new Thickness(4, 0)
            };
            button.Clicked += (_, _) => AdjustWeight(delta);
            QuickAdjustLayout.Children.Add(button);
        }
    }

    private void AdjustWeight(double delta)
    {
        var current = double.TryParse(WeightEntryInput.Text, out var value) ? value : 0;
        WeightEntryInput.Text = Math.Max(0, current + delta).ToString("0.0");
    }

    private void UpdateDaySummary(string unit)
    {
        if (dayReadings.Count > 1)
        {
            var averageKg = dayReadings.Average(reading => reading.WeightKg);
            var displayAverage = unit == "lb" ? averageKg / 0.45359237 : averageKg;
            DaySummaryLabel.Text = $"{dayReadings.Count} today · avg {displayAverage:0.0} {unit}";
            DaySummaryLabel.IsVisible = true;
        }
        else
        {
            DaySummaryLabel.IsVisible = false;
        }
    }

    private void UpdateDeltaLabel(string unit)
    {
        var previousDay = summaries.Where(summary => summary.Date < forDate).OrderByDescending(summary => summary.Date).FirstOrDefault();
        if (previousDay is null || dayReadings.Count == 0)
        {
            DeltaLabel.Text = string.Empty;
            return;
        }

        var todayAverageKg = dayReadings.Average(reading => reading.WeightKg);
        var deltaKg = todayAverageKg - previousDay.AverageWeightKg;
        var displayDelta = unit == "lb" ? deltaKg / 0.45359237 : deltaKg;
        var sign = displayDelta >= 0 ? "+" : string.Empty;
        DeltaLabel.Text = $"{sign}{displayDelta:0.0} {unit} vs previous day";
    }

    private async void OnRemoveReadingClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not int id)
            return;

        var reading = dayReadings.FirstOrDefault(entry => entry.Id == id);
        if (reading is null)
            return;

        await database.DeleteEntryAsync(reading);
        await LoadAsync();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!double.TryParse(WeightEntryInput.Text, out var enteredWeight) || enteredWeight <= 0)
        {
            await DisplayAlertAsync("Enter a weight", "Type a weight before saving.", "Done");
            return;
        }

        if (forDate > DateTime.Today)
        {
            await DisplayAlertAsync("Invalid date", "You can't log a future date.", "Done");
            return;
        }

        var unit = profile.WeightUnitPreference == "lb" ? "lb" : "kg";
        var weightKg = unit == "lb" ? enteredWeight * 0.45359237 : enteredWeight;

        if (weightKg is < 20 or > 400)
        {
            await DisplayAlertAsync("Check weight", "Enter a weight between 20 and 400 kg.", "Done");
            return;
        }

        var entry = new WeightEntry
        {
            DateTime = forDate == DateTime.Today ? DateTime.Now : forDate.AddHours(9),
            WeightKg = weightKg,
            HeightCmAtEntry = profile.HeightCm,
            Note = string.IsNullOrWhiteSpace(NoteEntry.Text) ? null : NoteEntry.Text.Trim()
        };
        await database.SaveEntryAsync(entry);

        var updatedCount = dayReadings.Count + 1;
        var updatedAverageKg = (dayReadings.Sum(reading => reading.WeightKg) + weightKg) / updatedCount;
        var displayAverage = unit == "lb" ? updatedAverageKg / 0.45359237 : updatedAverageKg;
        var toast = updatedCount == 1
            ? $"Logged {enteredWeight:0.0} {unit}."
            : $"Added. {updatedCount} weigh-ins today, average {displayAverage:0.0} {unit}.";
        await DisplayAlertAsync("Saved", toast, "Done");

        await Navigation.PopModalAsync();
    }

    private async void OnDeleteDayClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync("Delete day", $"Remove all {dayReadings.Count} weigh-ins for {DateLabel.Text}?", "Delete", "Cancel");
        if (!confirmed)
            return;

        foreach (var reading in dayReadings.ToList())
            await database.DeleteEntryAsync(reading);

        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
