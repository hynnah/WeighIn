using WeighIn.Controls;
using WeighIn.Services;

namespace WeighIn;

public partial class MainPage : ContentPage
{
	private readonly AppDatabase database = new();
	private readonly BmiGaugeDrawable gauge = new();
	private readonly ContributionGridDrawable contributionGrid = new();
	private readonly TrendSparklineDrawable sparkline = new();
	private readonly NavIconDrawable homeIcon = new() { Kind = NavIconKind.Home };
	private readonly NavIconDrawable trendsIcon = new() { Kind = NavIconKind.Trends };
	private readonly NavIconDrawable addIcon = new() { Kind = NavIconKind.Add, Color = Colors.White };
	private readonly NavIconDrawable calendarIcon = new() { Kind = NavIconKind.Calendar };
	private readonly NavIconDrawable moreIcon = new() { Kind = NavIconKind.More };
	private double lastBmi;
	private string lastBmiStandard = "Asian";

	public MainPage()
	{
		InitializeComponent();
		GaugeGraphic.Drawable = gauge;
		ContributionGraphic.Drawable = contributionGrid;
		TrendGraph.Drawable = sparkline;
		HomeIcon.Drawable = homeIcon;
		TrendsIcon.Drawable = trendsIcon;
		AddIcon.Drawable = addIcon;
		CalendarIcon.Drawable = calendarIcon;
		MoreIcon.Drawable = moreIcon;
		UpdateThemeButton();
	}

	private void UpdateNavIcons()
	{
		var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
		var muted = NavBarColors.Muted(isDark);
		homeIcon.Color = NavBarColors.Active;
		trendsIcon.Color = muted;
		calendarIcon.Color = muted;
		moreIcon.Color = muted;
		HomeLabel.TextColor = NavBarColors.Active;
		TrendsLabel.TextColor = muted;
		CalendarLabel.TextColor = muted;
		MoreLabel.TextColor = muted;
		HomeIcon.Invalidate();
		TrendsIcon.Invalidate();
		CalendarIcon.Invalidate();
		MoreIcon.Invalidate();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		UpdateNavIcons();
		await LoadDashboardAsync();
	}

	private async Task LoadDashboardAsync()
	{
		var profile = await database.GetProfileAsync();
		var summaries = await database.GetDailySummariesAsync(profile.HeightCm);
		var latestDay = summaries.FirstOrDefault();
		if (latestDay is null)
			return;

		var unit = profile.WeightUnitPreference == "lb" ? "lb" : "kg";
		var displayWeight = unit == "lb" ? latestDay.AverageWeightKg / 0.45359237 : latestDay.AverageWeightKg;
		var bmi = latestDay.AverageWeightKg / Math.Pow(latestDay.HeightCmAtEntry / 100, 2);
		var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
		CurrentWeightLabel.Text = displayWeight.ToString("0.0");
		CurrentUnitLabel.Text = unit;
		CurrentBmiLabel.Text = $"BMI {bmi:0.0}  ·  {BmiCalculator.GetCategory(bmi, profile.BmiStandard)}";
		CurrentBmiLabel.TextColor = BmiCalculator.GetCategoryColor(bmi, profile.BmiStandard, isDark);
		lastBmi = bmi;
		lastBmiStandard = profile.BmiStandard;
		BmiStandardLabel.Text = profile.BmiStandard == "General" ? "General standard" : "Asian standard";
		DashboardDateLabel.Text = latestDay.Date.ToString("ddd dd MMMM");

		var (overweightMax, obeseMax) = BmiCalculator.GetThresholds(profile.BmiStandard);
		gauge.Bmi = bmi;
		gauge.OverweightMax = overweightMax;
		gauge.ObeseMax = obeseMax;
		gauge.IsDark = isDark;
		gauge.MarkerFillColor = isDark ? Color.FromArgb("#1D1F2E") : Colors.White;
		gauge.MarkerRingColor = isDark ? Color.FromArgb("#E9E9ED") : Color.FromArgb("#182C2B");
		GaugeGraphic.Invalidate();

		DeltaSinceLastLabel.Text = FormatDelta(WeightStats.DeltaSinceLast(summaries), unit);
		DeltaSinceStartLabel.Text = FormatDelta(WeightStats.DeltaSinceStart(summaries), unit);

		var streak = WeightStats.ComputeStreak(summaries);
		StreakLabel.Text = streak == 1 ? "1 day" : $"{streak} days";

		var earliestDay = summaries[^1];
		var weeksTracking = Math.Max(1, (int)Math.Ceiling((DateTime.Today - earliestDay.Date).TotalDays / 7));
		WeeksTrackingLabel.Text = weeksTracking == 1 ? "1 week" : $"{weeksTracking} weeks";

		contributionGrid.LoggedDays = summaries.Select(summary => summary.Date).ToHashSet();
		ContributionGraphic.Invalidate();

		var last30 = summaries.Take(30).OrderBy(summary => summary.Date).ToList();
		var movingAverageByDate = WeightStats.MovingAverage(summaries).ToDictionary(point => point.Date, point => point.Average);
		var toDisplayUnit = unit == "lb" ? (Func<double, double>)(kg => kg / 0.45359237) : kg => kg;
		sparkline.Values = last30.Select(summary => toDisplayUnit(summary.AverageWeightKg)).ToList();
		sparkline.MovingAverage = last30
			.Select(summary => toDisplayUnit(movingAverageByDate.GetValueOrDefault(summary.Date, summary.AverageWeightKg)))
			.ToList();
		TrendGraph.Invalidate();
		TrendStartLabel.Text = last30.Count > 0 ? last30[0].Date.ToString("d MMM") : string.Empty;

		if (last30.Count > 1)
		{
			var weeks = (last30[^1].Date - last30[0].Date).TotalDays / 7;
			var weeklyRateKg = weeks > 0 ? (last30[^1].AverageWeightKg - last30[0].AverageWeightKg) / weeks : 0;
			var weeklyRateDisplay = toDisplayUnit(weeklyRateKg);
			var sign = weeklyRateDisplay > 0 ? "+" : string.Empty;
			WeeklyRateLabel.Text = $"{sign}{weeklyRateDisplay:0.0} {unit} / week";
		}
		else
		{
			WeeklyRateLabel.Text = "Not enough data";
		}
	}

	private static string FormatDelta(double? deltaKg, string unit)
	{
		if (deltaKg is null)
			return "—";

		var delta = unit == "lb" ? deltaKg.Value / 0.45359237 : deltaKg.Value;
		var sign = delta > 0 ? "+" : string.Empty;
		return $"{sign}{delta:0.0} {unit}";
	}

	private void OnThemeClicked(object? sender, EventArgs e)
	{
		if (Application.Current is null)
			return;

		Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Dark
			? AppTheme.Light
			: AppTheme.Dark;
		UpdateThemeButton();
	}

	private void UpdateThemeButton()
	{
		ThemeButton.Text = Application.Current?.UserAppTheme == AppTheme.Dark ? "☀" : "☾";
		ThemeButton.TextColor = Color.FromArgb("#9184D9");

		var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
		gauge.IsDark = isDark;
		gauge.MarkerFillColor = isDark ? Color.FromArgb("#1D1F2E") : Colors.White;
		gauge.MarkerRingColor = isDark ? Color.FromArgb("#E9E9ED") : Color.FromArgb("#182C2B");
		GaugeGraphic.Invalidate();
		CurrentBmiLabel.TextColor = BmiCalculator.GetCategoryColor(lastBmi, lastBmiStandard, isDark);

		contributionGrid.MissedColor = isDark ? Color.FromArgb("#232532") : Color.FromArgb("#E4E1D8");
		contributionGrid.FutureColor = isDark ? Color.FromRgba(35, 37, 50, 89) : Color.FromRgba(228, 225, 216, 140);
		ContributionGraphic.Invalidate();

		UpdateNavIcons();
	}

	private async void OnAddClicked(object? sender, TappedEventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());

	private async void OnHomeClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/home");
	private async void OnTrendsClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/trends");
	private async void OnCalendarClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/calendar");
	private async void OnMoreClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
