namespace WeighIn;

public partial class MorePage : ContentPage
{
    public MorePage()
    {
        InitializeComponent();
    }

    private async void OnHistoryTapped(object? sender, EventArgs e) => await Navigation.PushModalAsync(new HistoryPage());
    private async void OnSettingsTapped(object? sender, EventArgs e) => await Navigation.PushModalAsync(new SettingsPage());

    private async void OnHomeClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/home");
    private async void OnTrendsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/trends");
    private async void OnAddClicked(object? sender, EventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());
    private async void OnCalendarClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/calendar");
    private async void OnMoreClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
