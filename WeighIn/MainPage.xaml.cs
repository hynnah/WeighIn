namespace WeighIn;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
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
	private async void OnMoreClicked(object? sender, EventArgs e) => await DisplayActionSheetAsync("More", "Cancel", null, "History", "Settings", "Export data");
}
