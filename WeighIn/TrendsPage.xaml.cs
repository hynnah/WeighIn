using WeighIn.Controls;
using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn;

public partial class TrendsPage : ContentPage
{
    private static readonly (int Days, string Label)[] Ranges =
    [
        (7, "7D"),
        (30, "30D"),
        (90, "90D"),
        (365, "1Y"),
        (9999, "All")
    ];

    private readonly AppDatabase database = new();
    private readonly TrendsChartDrawable chart = new();
    private readonly NavIconDrawable homeIcon = new() { Kind = NavIconKind.Home };
    private readonly NavIconDrawable trendsIcon = new() { Kind = NavIconKind.Trends };
    private readonly NavIconDrawable addIcon = new() { Kind = NavIconKind.Add, Color = Colors.White };
    private readonly NavIconDrawable calendarIcon = new() { Kind = NavIconKind.Calendar };
    private readonly NavIconDrawable moreIcon = new() { Kind = NavIconKind.More };

    private List<DailySummary> allSummaries = [];
    private List<DailySummary> windowed = [];
    private Profile profile = new();
    private string metric = "weight";
    private int rangeDays = 30;
    private int? selectedIndex;
    private bool isDark;

    public TrendsPage()
    {
        InitializeComponent();
        ChartGraphic.Drawable = chart;
        HomeIcon.Drawable = homeIcon;
        TrendsIcon.Drawable = trendsIcon;
        AddIcon.Drawable = addIcon;
        CalendarIcon.Drawable = calendarIcon;
        MoreIcon.Drawable = moreIcon;
        BuildRangePills();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        chart.IsDark = isDark;

        var muted = NavBarColors.Muted(isDark);
        homeIcon.Color = muted;
        trendsIcon.Color = NavBarColors.Active;
        calendarIcon.Color = muted;
        moreIcon.Color = muted;
        HomeLabel.TextColor = muted;
        TrendsLabel.TextColor = NavBarColors.Active;
        CalendarLabel.TextColor = muted;
        MoreLabel.TextColor = muted;
        HomeIcon.Invalidate();
        TrendsIcon.Invalidate();
        CalendarIcon.Invalidate();
        MoreIcon.Invalidate();

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        profile = await database.GetProfileAsync();
        allSummaries = await database.GetDailySummariesAsync(profile.HeightCm);
        selectedIndex = null;
        RefreshRangePillStyles();
        RefreshMetricSegStyles();
        RebuildWindow();
    }

    private void BuildRangePills()
    {
        RangePillsGrid.Children.Clear();
        RangePillsGrid.ColumnDefinitions.Clear();
        for (var index = 0; index < Ranges.Length; index++)
            RangePillsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        for (var index = 0; index < Ranges.Length; index++)
        {
            var (days, label) = Ranges[index];
            var pill = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(0, 6),
                Content = new Label
                {
                    Text = label,
                    FontSize = 12,
                    HorizontalOptions = LayoutOptions.Center
                }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                rangeDays = days;
                selectedIndex = null;
                RefreshRangePillStyles();
                RebuildWindow();
            };
            pill.GestureRecognizers.Add(tap);
            Grid.SetColumn(pill, index);
            RangePillsGrid.Children.Add(pill);
        }

        RefreshRangePillStyles();
    }

    private void RefreshRangePillStyles()
    {
        for (var index = 0; index < RangePillsGrid.Children.Count; index++)
        {
            if (RangePillsGrid.Children[index] is not Border pill || pill.Content is not Label label)
                continue;

            var isActive = Ranges[index].Days == rangeDays;
            pill.BackgroundColor = isActive
                ? (isDark ? Color.FromArgb("#3A3266") : Color.FromArgb("#DCD5F5"))
                : (isDark ? Color.FromArgb("#1D1F2E") : Color.FromArgb("#FFFFFF"));
            label.TextColor = isActive
                ? (isDark ? Color.FromArgb("#D8D2FF") : Color.FromArgb("#4A3E8C"))
                : (isDark ? Color.FromArgb("#9397AB") : Color.FromArgb("#8D8A82"));
        }
    }

    private void RefreshMetricSegStyles()
    {
        var activeBg = isDark ? Color.FromArgb("#161826") : Color.FromArgb("#FFFFFF");
        var activeFg = Color.FromArgb("#9184D9");
        var inactiveFg = isDark ? Color.FromArgb("#9397AB") : Color.FromArgb("#8D8A82");

        var weightActive = metric == "weight";
        WeightSegBorder.BackgroundColor = weightActive ? activeBg : Colors.Transparent;
        WeightSegBorder.Stroke = weightActive ? activeFg : Colors.Transparent;
        WeightSegLabel.TextColor = weightActive ? activeFg : inactiveFg;

        var bmiActive = metric == "bmi";
        BmiSegBorder.BackgroundColor = bmiActive ? activeBg : Colors.Transparent;
        BmiSegBorder.Stroke = bmiActive ? activeFg : Colors.Transparent;
        BmiSegLabel.TextColor = bmiActive ? activeFg : inactiveFg;

        MetricUnitLabel.Text = metric == "bmi" ? "BMI" : (profile.WeightUnitPreference == "lb" ? "lb" : "kg");
    }

    private void RebuildWindow()
    {
        windowed = (rangeDays >= 9999
                ? allSummaries
                : allSummaries.Where(summary => summary.Date >= DateTime.Today.AddDays(-(rangeDays - 1))))
            .OrderBy(summary => summary.Date)
            .ToList();

        RenderChart();
    }

    private double ToDisplay(double kg)
    {
        if (metric == "bmi")
        {
            var heightM2 = Math.Pow(profile.HeightCm / 100, 2);
            return kg / heightM2;
        }

        return profile.WeightUnitPreference == "lb" ? kg / 0.45359237 : kg;
    }

    private string FormatDisplay(double value) => metric == "bmi" ? value.ToString("0.0") : value.ToString("0.0");

    private void RenderChart()
    {
        var unit = profile.WeightUnitPreference == "lb" ? "lb" : "kg";

        if (windowed.Count == 0)
        {
            chart.Values = [];
            chart.MovingAverage = [];
            chart.GoalValue = null;
            ChartGraphic.Invalidate();
            ChartFromLabel.Text = string.Empty;
            SelectedDateLabel.Text = "No entries in this range";
            SelectedValueLabel.Text = string.Empty;
            SelectedNoteLabel.Text = string.Empty;
            SetStat(StatCurrentValue, StatCurrentSub, "—", string.Empty);
            SetStat(StatStartValue, StatStartSub, "—", string.Empty);
            SetStat(StatChangeValue, StatChangeSub, "—", string.Empty);
            StatChangeValue.TextColor = isDark ? Color.FromArgb("#E9E9ED") : Color.FromArgb("#182C2B");
            SetStat(StatAverageValue, StatAverageSub, "—", string.Empty);
            SetStat(StatHighestValue, StatHighestSub, "—", string.Empty);
            SetStat(StatLowestValue, StatLowestSub, "—", string.Empty);
            VerdictArrowLabel.Text = "→";
            VerdictTextLabel.Text = "Not enough data in this range yet.";
            return;
        }

        var displayValues = windowed.Select(summary => ToDisplay(summary.AverageWeightKg)).ToList();
        var movingAverageKg = WeightStats.MovingAverage(windowed).Select(point => point.Average).ToList();
        var movingAverageDisplay = movingAverageKg.Select(ToDisplay).ToList();

        double? goalDisplay = profile.TargetWeightKg.HasValue ? ToDisplay(profile.TargetWeightKg.Value) : null;

        chart.Values = displayValues;
        chart.MovingAverage = movingAverageDisplay;
        chart.GoalValue = goalDisplay;
        chart.SelectedIndex = selectedIndex ?? windowed.Count - 1;
        chart.IsDark = isDark;
        ChartGraphic.Invalidate();

        ChartFromLabel.Text = windowed[0].Date.ToString("d MMM");

        var index = chart.SelectedIndex;
        var selected = windowed[index];
        SelectedDateLabel.Text = selected.Date.ToString("ddd d MMM");
        var selectedBmi = selected.AverageWeightKg / Math.Pow(selected.HeightCmAtEntry / 100, 2);
        var selectedDisplayWeight = profile.WeightUnitPreference == "lb" ? selected.AverageWeightKg / 0.45359237 : selected.AverageWeightKg;
        SelectedValueLabel.Text = $"{selectedDisplayWeight:0.0} {unit} · BMI {selectedBmi:0.0}";
        SelectedNoteLabel.Text = string.IsNullOrWhiteSpace(selected.Note) ? string.Empty : $"\"{selected.Note}\"";

        var current = displayValues[^1];
        var start = displayValues[0];
        var change = current - start;
        var average = displayValues.Average();
        var highest = displayValues.Max();
        var lowest = displayValues.Min();
        var highestDate = windowed[displayValues.IndexOf(highest)].Date;
        var lowestDate = windowed[displayValues.IndexOf(lowest)].Date;

        SetStat(StatCurrentValue, StatCurrentSub, FormatDisplay(current), string.Empty);
        SetStat(StatStartValue, StatStartSub, FormatDisplay(start), string.Empty);

        var changeSign = change <= 0 ? "−" : "+";
        SetStat(StatChangeValue, StatChangeSub, $"{changeSign}{Math.Abs(change):0.0}", string.Empty);
        StatChangeValue.TextColor = change <= 0 ? Color.FromArgb(isDark ? "#7FC39A" : "#3D8259") : Color.FromArgb(isDark ? "#E8CF95" : "#A87C2E");

        SetStat(StatAverageValue, StatAverageSub, FormatDisplay(average), string.Empty);
        SetStat(StatHighestValue, StatHighestSub, FormatDisplay(highest), highestDate.ToString("d MMM"));
        SetStat(StatLowestValue, StatLowestSub, FormatDisplay(lowest), lowestDate.ToString("d MMM"));

        RenderVerdict(unit);
    }

    private static void SetStat(Label valueLabel, Label subLabel, string value, string sub)
    {
        valueLabel.Text = value;
        subLabel.Text = sub;
    }

    private void RenderVerdict(string unit)
    {
        var weeklyRateKg = WeightStats.WeeklyRateKg(windowed);
        var weeklyRateDisplay = profile.WeightUnitPreference == "lb" ? weeklyRateKg / 0.45359237 : weeklyRateKg;

        if (weeklyRateKg < -0.02)
        {
            VerdictArrowLabel.Text = "↓";
            if (profile.TargetWeightKg.HasValue)
            {
                var weeksToGoal = (int)Math.Round((windowed[^1].AverageWeightKg - profile.TargetWeightKg.Value) / -weeklyRateKg);
                var goalDisplay = profile.WeightUnitPreference == "lb" ? profile.TargetWeightKg.Value / 0.45359237 : profile.TargetWeightKg.Value;
                VerdictTextLabel.Text = $"Losing steadily — {Math.Abs(weeklyRateDisplay):0.0} {unit} a week over this range. {weeksToGoal} weeks at this pace reaches {goalDisplay:0.0} {unit}.";
            }
            else
            {
                VerdictTextLabel.Text = $"Losing steadily — {Math.Abs(weeklyRateDisplay):0.0} {unit} a week over this range.";
            }
        }
        else if (weeklyRateKg > 0.02)
        {
            VerdictArrowLabel.Text = "↑";
            VerdictTextLabel.Text = $"Trending up {weeklyRateDisplay:0.0} {unit} a week over this range.";
        }
        else
        {
            VerdictArrowLabel.Text = "→";
            VerdictTextLabel.Text = "Holding steady over this range.";
        }
    }

    private void OnWeightMetricTapped(object? sender, EventArgs e)
    {
        metric = "weight";
        RefreshMetricSegStyles();
        RenderChart();
    }

    private void OnBmiMetricTapped(object? sender, EventArgs e)
    {
        metric = "bmi";
        RefreshMetricSegStyles();
        RenderChart();
    }

    private void OnChartTapped(object? sender, TappedEventArgs e)
    {
        if (windowed.Count < 2)
            return;

        var position = e.GetPosition(ChartGraphic);
        if (position is null || ChartGraphic.Width <= 0)
            return;

        var fraction = Math.Clamp(position.Value.X / ChartGraphic.Width, 0, 1);
        selectedIndex = (int)Math.Round(fraction * (windowed.Count - 1));
        RenderChart();
    }

    private async void OnHomeClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/home");
    private async void OnTrendsClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/trends");
    private async void OnAddClicked(object? sender, TappedEventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());
    private async void OnCalendarClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/calendar");
    private async void OnMoreClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
