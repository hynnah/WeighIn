using WeighIn.Services;

namespace WeighIn;

public partial class MainPage : ContentPage
{
	private readonly AppDatabase database = new();

	public MainPage()
	{
		InitializeComponent();
		TrendGraph.Drawable = new TrendDrawable();
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

internal sealed class TrendDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Color.FromArgb("#9184D9");
		canvas.StrokeSize = 2;
		canvas.StrokeLineCap = LineCap.Round;
		var points = new[]
		{
			new PointF(0, dirtyRect.Height * 0.32f),
			new PointF(dirtyRect.Width * 0.18f, dirtyRect.Height * 0.44f),
			new PointF(dirtyRect.Width * 0.36f, dirtyRect.Height * 0.28f),
			new PointF(dirtyRect.Width * 0.56f, dirtyRect.Height * 0.58f),
			new PointF(dirtyRect.Width * 0.76f, dirtyRect.Height * 0.48f),
			new PointF(dirtyRect.Width, dirtyRect.Height * 0.7f)
		};
		for (var index = 1; index < points.Length; index++)
			canvas.DrawLine(points[index - 1], points[index]);
	}
}
