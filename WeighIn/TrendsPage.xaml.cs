namespace WeighIn;

public partial class TrendsPage : ContentPage
{
    public TrendsPage()
    {
        InitializeComponent();
    }

    private async void OnHomeClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/home");
    private async void OnTrendsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/trends");
    private async void OnAddClicked(object? sender, EventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());
    private async void OnCalendarClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/calendar");
    private async void OnMoreClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
