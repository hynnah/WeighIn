namespace WeighIn;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		TrendGraph.Drawable = new TrendDrawable();
		UpdateThemeButton();
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

	private async void OnAddClicked(object? sender, EventArgs e)
	{
		var weight = await DisplayPromptAsync("Add weigh-in", "Weight in kg", "Add", "Cancel", "71.6", keyboard: Keyboard.Numeric);
		if (double.TryParse(weight, out var value) && value > 0)
			await DisplayAlertAsync("Saved", $"{value:0.0} kg added to today's average.", "Done");
	}

	private void OnHomeClicked(object? sender, EventArgs e) { }
	private async void OnTrendsClicked(object? sender, EventArgs e) => await DisplayAlertAsync("Trends", "Your 30-day trend is down 0.2 kg per week.", "Done");
	private async void OnCalendarClicked(object? sender, EventArgs e) => await DisplayAlertAsync("Calendar", "Calendar view is ready for your daily entries.", "Done");
	private async void OnMoreClicked(object? sender, EventArgs e)
	{
		var action = await DisplayActionSheetAsync("More", "Cancel", null, "History", "Settings", "Export data");
		if (action == "Settings")
			await Navigation.PushModalAsync(new SettingsPage());
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
