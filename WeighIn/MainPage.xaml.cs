using WeighIn.Controls;
using WeighIn.Services;

namespace WeighIn;

public partial class MainPage : ContentPage
{
	private readonly AppDatabase database = new();
	private readonly BmiGaugeDrawable gauge = new();
	private readonly ContributionGridDrawable contributionGrid = new();
	private readonly TrendSparklineDrawable sparkline = new();

	public MainPage()
	{
		InitializeComponent();
		GaugeGraphic.Drawable = gauge;
		ContributionGraphic.Drawable = contributionGrid;
		TrendGraph.Drawable = sparkline;
		UpdateThemeButton();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
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
		CurrentWeightLabel.Text = displayWeight.ToString("0.0");
		CurrentUnitLabel.Text = unit;
		CurrentBmiLabel.Text = $"BMI {bmi:0.0}  ·  {BmiCalculator.GetCategory(bmi, profile.BmiStandard)}";
		BmiStandardLabel.Text = profile.BmiStandard == "General" ? "General standard" : "Asian standard";
		DashboardDateLabel.Text = latestDay.Date.ToString("ddd dd MMMM");

		gauge.Bmi = bmi;
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

		gauge.TrackColor = Application.Current?.RequestedTheme == AppTheme.Dark
			? Color.FromArgb("#232532")
			: Color.FromArgb("#E4E1D8");
		GaugeGraphic.Invalidate();
	}

	private async void OnAddClicked(object? sender, EventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());

	private void OnHomeClicked(object? sender, EventArgs e) { }
	private async void OnTrendsClicked(object? sender, EventArgs e) => await DisplayAlertAsync("Trends", "Your 30-day trend is down 0.2 kg per week.", "Done");
	private async void OnCalendarClicked(object? sender, EventArgs e) => await DisplayAlertAsync("Calendar", "Calendar view is ready for your daily entries.", "Done");
	private async void OnMoreClicked(object? sender, EventArgs e)
	{
		var action = await DisplayActionSheetAsync("More", "Cancel", null, "History", "Settings", "Export data");
		if (action == "Settings")
			await Navigation.PushModalAsync(new SettingsPage());
		else if (action == "History")
			await Navigation.PushModalAsync(new HistoryPage());
	}
}
