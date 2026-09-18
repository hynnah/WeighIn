using WeighIn.Controls;
using WeighIn.Models;
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
	private readonly NavIconDrawable settingsIcon = new() { Kind = NavIconKind.Sliders };

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
		SettingsIcon.Drawable = settingsIcon;
	}

	private void UpdateNavIcons()
	{
		var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
		var muted = NavBarColors.Muted(isDark);
		homeIcon.Color = NavBarColors.Active;
		trendsIcon.Color = muted;
		calendarIcon.Color = muted;
		moreIcon.Color = muted;
		settingsIcon.Color = muted;
		HomeLabel.TextColor = NavBarColors.Active;
		TrendsLabel.TextColor = muted;
		CalendarLabel.TextColor = muted;
		MoreLabel.TextColor = muted;
		HomeIcon.Invalidate();
		TrendsIcon.Invalidate();
		CalendarIcon.Invalidate();
		MoreIcon.Invalidate();
		SettingsIcon.Invalidate();
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

		DateEyebrowLabel.Text = DateTime.Today.ToString("ddd d MMMM").ToUpperInvariant();
		GreetingLabel.Text = $"{TimeOfDayGreeting()}, Hannah";

		CurrentWeightLabel.Text = displayWeight.ToString("0.0");
		CurrentUnitLabel.Text = unit;

		var category = BmiCalculator.GetCategory(bmi, profile.BmiStandard);
		var categoryColor = BmiCalculator.GetCategoryColor(bmi, profile.BmiStandard, isDark);
		BmiValueSpan.Text = $"BMI {bmi:0.0} · ";
		BmiCategorySpan.Text = category;
		BmiCategorySpan.TextColor = categoryColor;

		var standardLabel = profile.BmiStandard == "General" ? "WHO standard" : "Asian standard";
		var toNormal = ToNormalText(bmi, profile.BmiStandard, latestDay.AverageWeightKg, latestDay.HeightCmAtEntry, unit);
		ToNormalLabel.Text = $"{standardLabel} · {toNormal}";

		gauge.Bmi = bmi;
		gauge.IsDark = isDark;
		gauge.MarkerFillColor = isDark ? Color.FromArgb("#1D1F2E") : Color.FromArgb("#F6F4EE");
		gauge.MarkerRingColor = isDark ? Color.FromArgb("#E9E9ED") : Color.FromArgb("#182C2B");
		GaugeGraphic.Invalidate();

		BuildWeekStrip(summaries, isDark);

		DeltaSinceLastLabel.Text = FormatDelta(WeightStats.DeltaSinceLast(summaries), unit);
		DeltaSinceStartLabel.Text = FormatDelta(WeightStats.DeltaSinceStart(summaries), unit);

		var streak = WeightStats.ComputeStreak(summaries);
		StreakLabel.Text = streak == 1 ? "1 day" : $"{streak} days";

		var earliestDay = summaries[^1];
		var weeksTracking = Math.Max(1, (int)Math.Ceiling((DateTime.Today - earliestDay.Date).TotalDays / 7));
		WeeksTrackingLabel.Text = weeksTracking == 1 ? "1 week" : $"{weeksTracking} weeks";

		contributionGrid.LoggedDays = summaries.Select(summary => summary.Date).ToHashSet();
		contributionGrid.MissedColor = isDark ? Color.FromArgb("#232532") : Color.FromArgb("#E4E1D8");
		contributionGrid.FutureColor = isDark ? Color.FromRgba(35, 37, 50, 89) : Color.FromRgba(228, 225, 216, 140);
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

	private static string TimeOfDayGreeting()
	{
		var hour = DateTime.Now.Hour;
		return hour < 12 ? "Morning" : hour < 18 ? "Afternoon" : "Evening";
	}

	private static string ToNormalText(double bmi, string standard, double weightKg, double heightCm, string unit)
	{
		var (overweightMax, _) = BmiCalculator.GetThresholds(standard);
		var heightM2 = Math.Pow(heightCm / 100, 2);

		double? diffKg = bmi < 18.5
			? 18.5 * heightM2 - weightKg
			: bmi >= overweightMax
				? weightKg - (overweightMax - 0.1) * heightM2
				: null;

		if (diffKg is null)
			return "in the healthy band";

		var displayDiff = unit == "lb" ? diffKg.Value / 0.45359237 : diffKg.Value;
		return $"{displayDiff:0.0} {unit} to Normal";
	}

	private void BuildWeekStrip(List<DailySummary> summaries, bool isDark)
	{
		var loggedDates = summaries.Select(summary => summary.Date).ToHashSet();
		var today = DateTime.Today;
		var weekStart = today.AddDays(-(int)today.DayOfWeek);

		WeekStripGrid.Children.Clear();
		for (var index = 0; index < 7; index++)
		{
			var date = weekStart.AddDays(index);
			var cell = BuildWeekCell(date, today, loggedDates.Contains(date), isDark);
			Grid.SetColumn(cell, index);
			WeekStripGrid.Children.Add(cell);
		}
	}

	private Grid BuildWeekCell(DateTime date, DateTime today, bool isLogged, bool isDark)
	{
		var isToday = date == today;
		var isFuture = date > today;

		Color circleBackground;
		Color circleBorder;
		Color numberColor;
		Color labelColor;
		var dashed = false;

		if (isLogged)
		{
			circleBackground = isDark ? Color.FromArgb("#3A3266") : Color.FromArgb("#DCD5F5");
			circleBorder = Colors.Transparent;
			numberColor = isDark ? Color.FromArgb("#D8D2FF") : Color.FromArgb("#4A3E8C");
			labelColor = isDark ? Color.FromArgb("#75798C") : Color.FromArgb("#8D8A82");
		}
		else if (isToday)
		{
			circleBackground = Colors.Transparent;
			circleBorder = Color.FromArgb("#9184D9");
			dashed = true;
			numberColor = Color.FromArgb("#9184D9");
			labelColor = Color.FromArgb("#9184D9");
		}
		else
		{
			circleBackground = Colors.Transparent;
			circleBorder = isDark ? Color.FromArgb("#2C2F3D") : Color.FromArgb("#D8D6D1");
			numberColor = isDark ? Color.FromArgb("#75798C") : Color.FromArgb("#9C9990");
			labelColor = isDark ? Color.FromArgb("#75798C") : Color.FromArgb("#9C9990");
		}

		var circle = new Border
		{
			WidthRequest = 30,
			HeightRequest = 30,
			BackgroundColor = circleBackground,
			Stroke = circleBorder,
			StrokeThickness = circleBorder == Colors.Transparent ? 0 : 1,
			StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
			Padding = 0,
			Content = new Label
			{
				Text = date.Day.ToString(),
				FontSize = 11,
				TextColor = numberColor,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			}
		};
		if (dashed)
			circle.StrokeDashArray = [2, 2];

		var label = new Label
		{
			Text = isToday ? "Today" : date.ToString("ddd"),
			FontSize = 10,
			TextColor = labelColor,
			HorizontalOptions = LayoutOptions.Center
		};

		var stack = new VerticalStackLayout { Spacing = 5, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
		stack.Children.Add(circle);
		stack.Children.Add(label);

		var cell = new Grid { HeightRequest = 56, BackgroundColor = Colors.Transparent };
		cell.Children.Add(stack);

		if (!isFuture)
		{
			var tap = new TapGestureRecognizer();
			tap.Tapped += async (_, _) => await Navigation.PushModalAsync(new LogSheetPage(date));
			cell.GestureRecognizers.Add(tap);
		}

		return cell;
	}

	private static string FormatDelta(double? deltaKg, string unit)
	{
		if (deltaKg is null)
			return "—";

		var delta = unit == "lb" ? deltaKg.Value / 0.45359237 : deltaKg.Value;
		var sign = delta > 0 ? "+" : string.Empty;
		return $"{sign}{delta:0.0} {unit}";
	}

	private async void OnAddClicked(object? sender, TappedEventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());

	private async void OnHomeClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/home");
	private async void OnTrendsClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/trends");
	private async void OnCalendarClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/calendar");
	private async void OnMoreClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
