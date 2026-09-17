using System.Globalization;
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

    public CalendarPage()
    {
        InitializeComponent();
        BuildWeekdayHeader();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var profile = await database.GetProfileAsync();
        weightUnit = profile.WeightUnitPreference == "lb" ? "lb" : "kg";
        var entries = await database.GetEntriesAsync();
        summariesByDate = DailySummary.FromEntries(entries).ToDictionary(summary => summary.Date, summary => summary);

        BuildDaysGrid();
        UpdateSelectedDayPanel();
    }

    private void BuildWeekdayHeader()
    {
        string[] labels = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];
        WeekdayHeaderGrid.Children.Clear();
        for (var index = 0; index < labels.Length; index++)
        {
            WeekdayHeaderGrid.Add(new Label
            {
                Text = labels[index],
                FontSize = 11,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Color.FromArgb("#75798C")
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
        var leadingBlanks = (int)displayedMonth.DayOfWeek;
        var rows = (int)Math.Ceiling((leadingBlanks + daysInMonth) / 7.0);
        for (var row = 0; row < rows; row++)
            DaysGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var today = DateTime.Today;
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(displayedMonth.Year, displayedMonth.Month, day);
            var cellIndex = leadingBlanks + day - 1;
            var row = cellIndex / 7;
            var column = cellIndex % 7;
            var isFuture = date > today;
            var isLogged = summariesByDate.ContainsKey(date);
            var isSelected = selectedDate == date;

            var cell = BuildDayCell(day, isFuture, isLogged, isSelected, isDark);
            if (!isFuture)
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => OnDaySelected(date);
                cell.GestureRecognizers.Add(tap);
            }

            DaysGrid.Add(cell, column, row);
        }
    }

    private static Border BuildDayCell(int day, bool isFuture, bool isLogged, bool isSelected, bool isDark)
    {
        var normalTextColor = isDark ? Color.FromArgb("#E9E9ED") : Color.FromArgb("#182C2B");
        var futureTextColor = Color.FromArgb("#75798C");
        var textColor = isFuture ? futureTextColor : normalTextColor;
        var normalBackground = isDark ? Color.FromArgb("#1D1F2E") : Color.FromArgb("#FFFFFF");
        var background = isSelected ? Color.FromArgb("#9184D9") : normalBackground;

        var stack = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Center };
        stack.Children.Add(new Label
        {
            Text = day.ToString(),
            FontSize = 13,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = isSelected ? Colors.White : textColor,
            Opacity = isFuture ? 0.4 : 1
        });
        var dotColor = isLogged
            ? (isSelected ? Colors.White : Color.FromArgb("#9184D9"))
            : background;
        stack.Children.Add(new BoxView
        {
            WidthRequest = 4,
            HeightRequest = 4,
            CornerRadius = 2,
            Color = dotColor,
            BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Center
        });

        var stroke = isSelected ? Colors.Transparent : isDark ? Color.FromArgb("#2C2F3D") : Color.FromArgb("#D8D6D1");

        return new Border
        {
            BackgroundColor = background,
            Stroke = stroke,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(0, 6),
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
        if (selectedDate is null)
        {
            SelectedDateLabel.Text = "Select a day";
            SelectedWeightLabel.Text = string.Empty;
            LogSelectedDayButton.Text = "Log this day";
            return;
        }

        var date = selectedDate.Value;
        SelectedDateLabel.Text = date == DateTime.Today ? "Today" : date.ToString("dddd, dd MMMM");

        if (summariesByDate.TryGetValue(date, out var summary))
        {
            var displayWeight = weightUnit == "lb" ? summary.AverageWeightKg / 0.45359237 : summary.AverageWeightKg;
            SelectedWeightLabel.Text = summary.Readings.Count > 1
                ? $"avg of {summary.Readings.Count} weigh-ins · {displayWeight:0.0} {weightUnit}"
                : $"{displayWeight:0.0} {weightUnit} · {summary.Readings[0].DateTime:HH:mm}";
            LogSelectedDayButton.Text = "Add another weigh-in";
        }
        else
        {
            SelectedWeightLabel.Text = "No entry logged";
            LogSelectedDayButton.Text = "Log this day";
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

    private async void OnHomeClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/home");
    private async void OnTrendsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/trends");
    private async void OnAddClicked(object? sender, EventArgs e) => await Navigation.PushModalAsync(new LogSheetPage());
    private async void OnCalendarClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/calendar");
    private async void OnMoreClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/more");
}
