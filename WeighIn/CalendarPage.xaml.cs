using System.Globalization;
using WeighIn.Controls;
using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn;

public partial class CalendarPage : ContentPage
{
    private readonly AppDatabase database = new();
    private DateTime displayedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? selectedDate;
    private Dictionary<DateTime, DailySummary> summariesByDate = new();
    private string weightUnit = "kg";
    private string bmiStandard = "Asian";
    private readonly NavIconDrawable homeIcon = new() { Kind = NavIconKind.Home };
    private readonly NavIconDrawable trendsIcon = new() { Kind = NavIconKind.Trends };
    private readonly NavIconDrawable addIcon = new() { Kind = NavIconKind.Add, Color = Colors.White };
    private readonly NavIconDrawable calendarIcon = new() { Kind = NavIconKind.Calendar };
    private readonly NavIconDrawable moreIcon = new() { Kind = NavIconKind.More };

    public CalendarPage()
    {
        InitializeComponent();
        BuildWeekdayHeader();
        HomeIcon.Drawable = homeIcon;
        TrendsIcon.Drawable = trendsIcon;
        AddIcon.Drawable = addIcon;
        CalendarIcon.Drawable = calendarIcon;
        MoreIcon.Drawable = moreIcon;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateNavIcons();
        await LoadAsync();
    }

    private void UpdateNavIcons()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var muted = NavBarColors.Muted(isDark);
        homeIcon.Color = muted;
        trendsIcon.Color = muted;
        calendarIcon.Color = NavBarColors.Active;
        moreIcon.Color = muted;
        HomeLabel.TextColor = muted;
        TrendsLabel.TextColor = muted;
        CalendarLabel.TextColor = NavBarColors.Active;
        MoreLabel.TextColor = muted;
        HomeIcon.Invalidate();
        TrendsIcon.Invalidate();
        CalendarIcon.Invalidate();
        MoreIcon.Invalidate();
    }

    private async Task LoadAsync()
    {
        var profile = await database.GetProfileAsync();
        weightUnit = profile.WeightUnitPreference == "lb" ? "lb" : "kg";
        bmiStandard = profile.BmiStandard;
        var entries = await database.GetEntriesAsync();
        summariesByDate = DailySummary.FromEntries(entries).ToDictionary(summary => summary.Date, summary => summary);

        BuildDaysGrid();
        UpdateSelectedDayPanel();
    }

    private void BuildWeekdayHeader()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var mutedColor = isDark ? Color.FromArgb("#75798C") : Color.FromArgb("#8D8A82");
        string[] labels = ["M", "T", "W", "T", "F", "S", "S"];
        WeekdayHeaderGrid.Children.Clear();
        for (var index = 0; index < labels.Length; index++)
        {
            WeekdayHeaderGrid.Add(new Label
            {
                Text = labels[index],
                FontSize = 11,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = mutedColor
            }, index, 0);
        }
    }

    private void BuildDaysGrid()
    {
        MonthLabel.Text = displayedMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        DaysGrid.Children.Clear();
        DaysGrid.RowDefinitions.Clear();
        DaysGrid.ColumnDefinitions.Clear();
        for (var column = 0; column < 7; column++)
            DaysGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var daysInMonth = DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month);
        var leadingBlanks = ((int)displayedMonth.DayOfWeek + 6) % 7;
        var rows = (int)Math.Ceiling((leadingBlanks + daysInMonth) / 7.0);
        for (var row = 0; row < rows; row++)
            DaysGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var today = DateTime.Today;
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var elapsed = 0;
        var loggedCount = 0;

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(displayedMonth.Year, displayedMonth.Month, day);
            var cellIndex = leadingBlanks + day - 1;
            var row = cellIndex / 7;
            var column = cellIndex % 7;
            var isFuture = date > today;
            var isLogged = summariesByDate.TryGetValue(date, out var summary);
            var isSelected = selectedDate == date;
            var isToday = date == today;

            if (!isFuture)
            {
                elapsed++;
                if (isLogged)
                    loggedCount++;
            }

            var displayWeight = isLogged
                ? (weightUnit == "lb" ? summary!.AverageWeightKg / 0.45359237 : summary!.AverageWeightKg)
                : (double?)null;

            var cell = BuildDayCell(day, isFuture, isLogged, isSelected, isToday, isDark, displayWeight, weightUnit);
            if (!isFuture)
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => OnDaySelected(date);
                cell.GestureRecognizers.Add(tap);
            }

            DaysGrid.Add(cell, column, row);
        }

        MonthSubtitleLabel.Text = $"{loggedCount} of {elapsed} days logged";
    }

    private static Border BuildDayCell(int day, bool isFuture, bool isLogged, bool isSelected, bool isToday, bool isDark, double? displayWeight, string weightUnit)
    {
        var loggedBackground = isDark ? Color.FromArgb("#423A6A") : Color.FromArgb("#DCD5F5");
        var missedBackground = isDark ? Color.FromArgb("#232532") : Color.FromArgb("#E4E1D8");
        var futureBackground = isDark ? Color.FromRgba(35, 37, 50, 89) : Color.FromRgba(228, 225, 216, 55);
        var normalTextColor = isDark ? Color.FromArgb("#E9E9ED") : Color.FromArgb("#182C2B");
        var mutedTextColor = isDark ? Color.FromArgb("#75798C") : Color.FromArgb("#8D8A82");

        var background = isSelected ? Color.FromArgb("#9184D9") : isFuture ? futureBackground : isLogged ? loggedBackground : missedBackground;
        var textColor = isSelected ? Colors.White : isFuture ? mutedTextColor : normalTextColor;
        var weightColor = isSelected ? Colors.White : mutedTextColor;

        var stack = new VerticalStackLayout { Spacing = 1, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        stack.Children.Add(new Label
        {
            Text = day.ToString(),
            FontSize = 13,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = textColor,
            Opacity = isFuture ? 0.55 : 1
        });
        stack.Children.Add(new Label
        {
            Text = displayWeight.HasValue ? $"{displayWeight:0.#}" : " ",
            FontSize = 8,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = weightColor,
            Opacity = 0.85
        });

        var stroke = isToday && !isSelected ? Color.FromArgb("#9184D9") : Colors.Transparent;
        var strokeThickness = isToday && !isSelected ? 2 : 0;

        return new Border
        {
            BackgroundColor = background,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(0, 6),
            HeightRequest = 44,
            Content = stack
        };
    }

    private void OnDaySelected(DateTime date)
    {
        selectedDate = date;
        BuildDaysGrid();
        UpdateSelectedDayPanel();
    }

    private void UpdateSelectedDayPanel()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        if (selectedDate is null)
        {
            SelectedDateLabel.Text = "Select a day";
            SelectedTagLabel.Text = string.Empty;
            SelectedWeightLabel.Text = string.Empty;
            SelectedBmiLabel.Text = string.Empty;
            SelectedNoteLabel.Text = string.Empty;
            LogSelectedDayButton.Text = "Log this day";
            return;
        }

        var date = selectedDate.Value;
        var today = DateTime.Today;
        SelectedDateLabel.Text = date.ToString("dddd, dd MMMM");
        SelectedTagLabel.Text = date == today ? "Today" : date == today.AddDays(-1) ? "Yesterday" : date.ToString("ddd").ToUpperInvariant();

        if (summariesByDate.TryGetValue(date, out var summary))
        {
            var displayWeight = weightUnit == "lb" ? summary.AverageWeightKg / 0.45359237 : summary.AverageWeightKg;
            SelectedWeightLabel.Text = summary.Readings.Count > 1
                ? $"avg of {summary.Readings.Count} weigh-ins · {displayWeight:0.0} {weightUnit}"
                : $"{displayWeight:0.0} {weightUnit} · {summary.Readings[0].DateTime:HH:mm}";

            var bmi = summary.AverageWeightKg / Math.Pow(summary.HeightCmAtEntry / 100, 2);
            SelectedBmiLabel.Text = $"BMI {bmi:0.0}";
            SelectedBmiLabel.TextColor = BmiCalculator.GetCategoryColor(bmi, bmiStandard, isDark);

            SelectedNoteLabel.Text = string.IsNullOrWhiteSpace(summary.Note) ? string.Empty : $"\"{summary.Note}\"";
            LogSelectedDayButton.Text = "Add another weigh-in";
        }
        else
        {
            SelectedWeightLabel.Text = "No entry for this day.";
            SelectedBmiLabel.Text = string.Empty;
            SelectedNoteLabel.Text = string.Empty;
            LogSelectedDayButton.Text = "Add a weight";
        }
    }

    private async void OnLogSelectedDayClicked(object? sender, EventArgs e)
    {
        if (selectedDate is null)
            return;

        await Navigation.PushModalAsync(new LogSheetPage(selectedDate));
    }

    private void OnPreviousMonthClicked(object? sender, EventArgs e)
    {
        displayedMonth = displayedMonth.AddMonths(-1);
        BuildDaysGrid();
    }

    private void OnNextMonthClicked(object? sender, EventArgs e)
    {
        displayedMonth = displayedMonth.AddMonths(1);
        BuildDaysGrid();
    }

    private async void OnHomeClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/home");
    private async void OnTrendsClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/trends");
    private async void OnAddClicked(object? sender, TappedEventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());
    private async void OnCalendarClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/calendar");
    private async void OnMoreClicked(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
