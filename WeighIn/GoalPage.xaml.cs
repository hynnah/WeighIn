using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn;

public partial class GoalPage : ContentPage
{
    private static readonly int[] PresetWeeks = [8, 12, 16, 24, 36];

    private readonly AppDatabase database = new();
    private Profile profile = new();
    private double currentWeightKg;
    private double weeklyRateKg;
    private double workingGoalKg;
    private DateTime workingGoalDate;
    private string unit = "kg";
    private bool isDark;

    public GoalPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        profile = await database.GetProfileAsync();
        var summaries = await database.GetDailySummariesAsync();
        unit = profile.WeightUnitPreference == "lb" ? "lb" : "kg";

        if (summaries.Count == 0)
        {
            currentWeightKg = profile.TargetWeightKg ?? 60;
        }
        else
        {
            currentWeightKg = summaries[0].AverageWeightKg;
            var last30 = summaries.Take(30).OrderBy(summary => summary.Date).ToList();
            weeklyRateKg = WeightStats.WeeklyRateKg(last30);
        }

        workingGoalKg = profile.TargetWeightKg ?? Math.Round(Math.Max(30, currentWeightKg - 2), 1);
        workingGoalDate = profile.TargetDate ?? DateTime.Today.AddDays(PresetWeeks[1] * 7);

        CustomDatePicker.MinimumDate = DateTime.Today;

        BuildDateChips();
        Render();
    }

    private double ToDisplay(double kg) => unit == "lb" ? kg / 0.45359237 : kg;
    private double StepKg => unit == "lb" ? 0.1 / 2.20462 : 0.1;

    private void BuildDateChips()
    {
        DateChipsStack.Children.Clear();
        foreach (var weeks in PresetWeeks)
        {
            var date = DateTime.Today.AddDays(weeks * 7);
            var chip = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(12, 8),
                BindingContext = date,
                Content = new VerticalStackLayout
                {
                    Spacing = 1,
                    Children =
                    {
                        new Label { Text = date.ToString("d MMM"), FontSize = 13, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = $"{weeks}w", FontSize = 10, Opacity = 0.7, HorizontalOptions = LayoutOptions.Center }
                    }
                }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                workingGoalDate = date;
                Render();
            };
            chip.GestureRecognizers.Add(tap);
            DateChipsStack.Children.Add(chip);
        }
    }

    private void RefreshDateChipStyles()
    {
        foreach (var child in DateChipsStack.Children)
        {
            if (child is not Border { BindingContext: DateTime chipDate } chip || chip.Content is not VerticalStackLayout stack)
                continue;

            var dateLabel = (Label)stack.Children[0];
            var weekLabel = (Label)stack.Children[1];
            var isSelected = chipDate.Date == workingGoalDate.Date;

            chip.BackgroundColor = isSelected
                ? (isDark ? Color.FromArgb("#3A3266") : Color.FromArgb("#DCD5F5"))
                : (isDark ? Color.FromArgb("#1D1F2E") : Color.FromArgb("#FFFFFF"));
            var fg = isSelected
                ? (isDark ? Color.FromArgb("#D8D2FF") : Color.FromArgb("#4A3E8C"))
                : (isDark ? Color.FromArgb("#9397AB") : Color.FromArgb("#8D8A82"));
            dateLabel.TextColor = fg;
            weekLabel.TextColor = fg;
        }
    }

    private void Render()
    {
        GoalWeightLabel.Text = ToDisplay(workingGoalKg).ToString("0.0");
        GoalUnitLabel.Text = unit;

        var diffKg = currentWeightKg - workingGoalKg;
        var diffDisplay = Math.Abs(ToDisplay(diffKg));
        GoalDeltaLabel.Text = Math.Abs(diffKg) < 0.05
            ? "You're already at your goal weight."
            : diffKg > 0
                ? $"{diffDisplay:0.0} {unit} below where you are today"
                : $"{diffDisplay:0.0} {unit} above where you are today";

        RefreshDateChipStyles();
        if (CustomDatePicker.Date != workingGoalDate.Date)
            CustomDatePicker.Date = workingGoalDate.Date;

        var goalWeeks = Math.Max(0, (int)Math.Round((workingGoalDate.Date - DateTime.Today).TotalDays / 7));
        var neededPaceKg = goalWeeks > 0 ? diffKg / goalWeeks : 0;
        NeededPaceLabel.Text = ToDisplay(neededPaceKg).ToString("0.00");
        NeededPaceUnitLabel.Text = $"{unit}/wk";
        CurrentPaceLabel.Text = ToDisplay(Math.Abs(weeklyRateKg)).ToString("0.00");
        CurrentPaceUnitLabel.Text = $"{unit}/wk";

        var warm = Color.FromArgb(isDark ? "#E0A08C" : "#B15A3E");
        var good = Color.FromArgb(isDark ? "#7FC39A" : "#3D8259");
        var caution = Color.FromArgb(isDark ? "#E8CF95" : "#A87C2E");

        if (goalWeeks <= 0)
        {
            VerdictLabel.Text = "Pick a date in the future.";
            VerdictLabel.TextColor = warm;
        }
        else if (neededPaceKg > 1.0)
        {
            VerdictLabel.Text = "That is faster than 1.0 kg a week. Consider a later date.";
            VerdictLabel.TextColor = warm;
        }
        else if (neededPaceKg <= Math.Abs(weeklyRateKg) + 0.02)
        {
            VerdictLabel.Text = "Your current pace reaches this comfortably.";
            VerdictLabel.TextColor = good;
        }
        else
        {
            VerdictLabel.Text = "Slightly ahead of your current pace. Reachable, but tight.";
            VerdictLabel.TextColor = caution;
        }
    }

    private void OnCustomDateSelected(object? sender, DateChangedEventArgs e)
    {
        var newDate = e.NewDate.GetValueOrDefault(workingGoalDate).Date;
        if (newDate == workingGoalDate.Date)
            return;

        workingGoalDate = newDate;
        Render();
    }

    private void OnGoalUpTapped(object? sender, EventArgs e)
    {
        workingGoalKg = Math.Round((workingGoalKg + StepKg) * 10) / 10;
        Render();
    }

    private void OnGoalDownTapped(object? sender, EventArgs e)
    {
        workingGoalKg = Math.Max(30, Math.Round((workingGoalKg - StepKg) * 10) / 10);
        Render();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        profile.TargetWeightKg = workingGoalKg;
        profile.TargetDate = workingGoalDate.Date;
        await database.SaveProfileAsync(profile);
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
