using WeighIn.Controls;

namespace WeighIn;

public partial class MorePage : ContentPage
{
    private readonly NavIconDrawable homeIcon = new() { Kind = NavIconKind.Home };
    private readonly NavIconDrawable trendsIcon = new() { Kind = NavIconKind.Trends };
    private readonly NavIconDrawable addIcon = new() { Kind = NavIconKind.Add, Color = Colors.White };
    private readonly NavIconDrawable calendarIcon = new() { Kind = NavIconKind.Calendar };
    private readonly NavIconDrawable moreIcon = new() { Kind = NavIconKind.More };

    public MorePage()
    {
        InitializeComponent();
        HomeIcon.Drawable = homeIcon;
        TrendsIcon.Drawable = trendsIcon;
        AddIcon.Drawable = addIcon;
        CalendarIcon.Drawable = calendarIcon;
        MoreIcon.Drawable = moreIcon;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        DarkModeSwitch.IsToggled = isDark;
        var muted = NavBarColors.Muted(isDark);
        homeIcon.Color = muted;
        trendsIcon.Color = muted;
        calendarIcon.Color = muted;
        moreIcon.Color = NavBarColors.Active;
        HomeLabel.TextColor = muted;
        TrendsLabel.TextColor = muted;
        CalendarLabel.TextColor = muted;
        MoreLabel.TextColor = NavBarColors.Active;
        HomeIcon.Invalidate();
        TrendsIcon.Invalidate();
        CalendarIcon.Invalidate();
        MoreIcon.Invalidate();
    }

    private async void OnHistoryTapped(object? sender, EventArgs e) => await Navigation.PushModalAsync(new HistoryPage());
    private async void OnSettingsTapped(object? sender, EventArgs e) => await Navigation.PushModalAsync(new SettingsPage());

    private void OnDarkModeToggled(object? sender, ToggledEventArgs e)
    {
        if (Application.Current is null)
            return;

        Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    private async void OnHomeClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/home");
    private async void OnTrendsClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/trends");
    private async void OnAddClicked(object? sender, TappedEventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());
    private async void OnCalendarClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/calendar");
    private async void OnMoreClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
