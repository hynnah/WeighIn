using WeighIn.Controls;

namespace WeighIn;

public partial class TrendsPage : ContentPage
{
    private readonly NavIconDrawable homeIcon = new() { Kind = NavIconKind.Home };
    private readonly NavIconDrawable trendsIcon = new() { Kind = NavIconKind.Trends };
    private readonly NavIconDrawable addIcon = new() { Kind = NavIconKind.Add, Color = Colors.White };
    private readonly NavIconDrawable calendarIcon = new() { Kind = NavIconKind.Calendar };
    private readonly NavIconDrawable moreIcon = new() { Kind = NavIconKind.More };

    public TrendsPage()
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
    }

    private async void OnHomeClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/home");
    private async void OnTrendsClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/trends");
    private async void OnAddClicked(object? sender, TappedEventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());
    private async void OnCalendarClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/calendar");
    private async void OnMoreClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
