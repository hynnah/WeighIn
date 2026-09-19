using WeighIn.Services;

namespace WeighIn;

public partial class HistoryPage : ContentPage
{
    private static readonly (string Key, string Label)[] Filters =
    [
        ("month", "This month"),
        ("last60", "Last 60 days"),
        ("notes", "With notes")
    ];

    private readonly AppDatabase database = new();
    private List<DailySummary> summaries = [];
    private string unit = "kg";
    private string standard = "Asian";
    private string activeFilter = "month";
    private string query = string.Empty;
    private bool isDark;

    public HistoryPage()
    {
        InitializeComponent();
        BuildFilterChips();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var profile = await database.GetProfileAsync();
        unit = profile.WeightUnitPreference == "lb" ? "lb" : "kg";
        standard = profile.BmiStandard;
        summaries = await database.GetDailySummariesAsync(profile.HeightCm);

        CountLabel.Text = summaries.Count == 1 ? "1 entry" : $"{summaries.Count} entries";
        RefreshFilterChipStyles();
        Render();
    }

    private void BuildFilterChips()
    {
        FilterChipsGrid.Children.Clear();
        FilterChipsGrid.ColumnDefinitions.Clear();
        foreach (var _ in Filters)
            FilterChipsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        for (var index = 0; index < Filters.Length; index++)
        {
            var (key, label) = Filters[index];
            var chip = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(11, 6),
                BindingContext = key,
                Content = new Label { Text = label, FontSize = 12 }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                activeFilter = key;
                RefreshFilterChipStyles();
                Render();
            };
            chip.GestureRecognizers.Add(tap);
            Grid.SetColumn(chip, index);
            FilterChipsGrid.Children.Add(chip);
        }
    }

    private void RefreshFilterChipStyles()
    {
        foreach (var child in FilterChipsGrid.Children)
        {
            if (child is not Border { BindingContext: string key } chip || chip.Content is not Label label)
                continue;

            var isActive = key == activeFilter;
            chip.BackgroundColor = isActive
                ? (isDark ? Color.FromArgb("#3A3266") : Color.FromArgb("#DCD5F5"))
                : (isDark ? Color.FromArgb("#1D1F2E") : Color.FromArgb("#FFFFFF"));
            label.TextColor = isActive
                ? (isDark ? Color.FromArgb("#D8D2FF") : Color.FromArgb("#4A3E8C"))
                : (isDark ? Color.FromArgb("#9397AB") : Color.FromArgb("#8D8A82"));
        }
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        query = (e.NewTextValue ?? string.Empty).Trim();
        Render();
    }

    private void Render()
    {
        var byDate = summaries.ToDictionary(summary => summary.Date);
        var today = DateTime.Today;
        var rows = new List<HistoryRowView>();

        if (activeFilter == "notes")
        {
            SectionHeadLabel.Text = "ENTRIES WITH NOTES";
            foreach (var summary in summaries.Where(summary => !string.IsNullOrWhiteSpace(summary.Note)))
                rows.Add(HistoryRowView.FromSummary(summary, unit, standard));
        }
        else
        {
            DateTime start;
            if (activeFilter == "month")
            {
                start = new DateTime(today.Year, today.Month, 1);
                SectionHeadLabel.Text = today.ToString("MMMM yyyy").ToUpperInvariant();
            }
            else
            {
                start = today.AddDays(-59);
                SectionHeadLabel.Text = "LAST 60 DAYS";
            }

            for (var date = start; date <= today; date = date.AddDays(1))
            {
                rows.Add(byDate.TryGetValue(date, out var summary)
                    ? HistoryRowView.FromSummary(summary, unit, standard)
                    : HistoryRowView.Empty(date));
            }

            rows.Reverse();
        }

        if (!string.IsNullOrEmpty(query))
        {
            var q = query.ToLowerInvariant();
            rows = rows.Where(row => row.SearchHaystack.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var realValues = rows.Where(row => row.HasEntry).Select(row => row.WeightKg).ToList();
        SectionAvgLabel.Text = realValues.Count > 0
            ? $"avg {(unit == "lb" ? realValues.Average() / 0.45359237 : realValues.Average()):0.0} {unit}"
            : "no entries";

        RowsView.ItemsSource = rows;
    }

    private async void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is HistoryRowView row)
            await Navigation.PushModalAsync(new LogSheetPage(row.Date));
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}

internal sealed class HistoryRowView
{
    public required DateTime Date { get; init; }
    public required bool HasEntry { get; init; }
    public required double WeightKg { get; init; }
    public required string DayNumber { get; init; }
    public required string DayOfWeek { get; init; }
    public required string WeightText { get; init; }
    public required string MetaText { get; init; }
    public required string NoteText { get; init; }
    public required FontAttributes NoteStyle { get; init; }
    public required string BmiText { get; init; }
    public required string BmiLabel { get; init; }
    public required Color BmiColor { get; init; }
    public required double Opacity { get; init; }
    public required string SearchHaystack { get; init; }

    public static HistoryRowView FromSummary(DailySummary summary, string unit, string standard)
    {
        var displayWeight = unit == "lb" ? summary.AverageWeightKg / 0.45359237 : summary.AverageWeightKg;
        var bmi = summary.AverageWeightKg / Math.Pow(summary.HeightCmAtEntry / 100, 2);
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var hasNote = !string.IsNullOrWhiteSpace(summary.Note);

        return new HistoryRowView
        {
            Date = summary.Date,
            HasEntry = true,
            WeightKg = summary.AverageWeightKg,
            DayNumber = summary.Date.Day.ToString(),
            DayOfWeek = summary.Date.ToString("ddd").ToUpperInvariant(),
            WeightText = $"{displayWeight:0.0} {unit}",
            MetaText = summary.Readings.Count > 1 ? $"avg of {summary.Readings.Count}" : summary.Readings[^1].DateTime.ToString("h:mm tt"),
            NoteText = hasNote ? $"\"{summary.Note}\"" : string.Empty,
            NoteStyle = hasNote ? FontAttributes.Italic : FontAttributes.None,
            BmiText = bmi.ToString("0.0"),
            BmiLabel = "BMI",
            BmiColor = BmiCalculator.GetCategoryColor(bmi, standard, isDark),
            Opacity = 1.0,
            SearchHaystack = $"{summary.Note} {summary.AverageWeightKg} {summary.Date:d MMMM} {summary.Date:ddd}"
        };
    }

    public static HistoryRowView Empty(DateTime date) => new()
    {
        Date = date,
        HasEntry = false,
        WeightKg = 0,
        DayNumber = date.Day.ToString(),
        DayOfWeek = date.ToString("ddd").ToUpperInvariant(),
        WeightText = "—",
        MetaText = string.Empty,
        NoteText = "No entry — tap to add",
        NoteStyle = FontAttributes.None,
        BmiText = string.Empty,
        BmiLabel = string.Empty,
        BmiColor = Colors.Transparent,
        Opacity = 0.5,
        SearchHaystack = $"{date:d MMMM} {date:ddd}"
    };
}
