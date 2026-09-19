using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn;

public partial class WaistHipsPage : ContentPage
{
    private readonly AppDatabase database = new();
    private List<BodyMeasurement> measurements = [];
    private string unit = "cm";

    public WaistHipsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var profile = await database.GetProfileAsync();
        unit = profile.WeightUnitPreference == "lb" ? "in" : "cm";
        measurements = await database.GetBodyMeasurementsAsync();

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        if (measurements.Count > 0)
        {
            var latest = measurements[0];
            var ratio = WaistHipsCalculator.Ratio(latest.WaistCm, latest.HipsCm);
            var category = WaistHipsCalculator.GetCategory(ratio);
            RatioValueLabel.Text = ratio.ToString("0.00");
            RatioCategoryLabel.Text = category;
            RatioCategoryLabel.TextColor = WaistHipsCalculator.GetCategoryColor(ratio, isDark);
            LatestDetailLabel.Text = $"{ToDisplay(latest.WaistCm):0.0} / {ToDisplay(latest.HipsCm):0.0} {unit} waist/hips · {latest.DateTime:dd MMM yyyy}";
        }
        else
        {
            RatioValueLabel.Text = "—";
            RatioCategoryLabel.Text = string.Empty;
            LatestDetailLabel.Text = "Log a measurement to see your ratio.";
        }

        MeasurementsView.ItemsSource = measurements.Select(measurement => MeasurementRowView.From(measurement, unit, isDark)).ToList();
    }

    private double ToDisplay(double cm) => unit == "in" ? cm / 2.54 : cm;

    private async void OnLogClicked(object? sender, EventArgs e) => await Navigation.PushModalAsync(new LogMeasurementPage());

    private async void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is MeasurementRowView row)
            await Navigation.PushModalAsync(new LogMeasurementPage(row.Measurement));
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}

internal sealed class MeasurementRowView
{
    public required BodyMeasurement Measurement { get; init; }
    public required string DayNumber { get; init; }
    public required string MonthAbbrev { get; init; }
    public required string WaistHipsText { get; init; }
    public required string NoteText { get; init; }
    public required string RatioText { get; init; }
    public required Color RatioColor { get; init; }

    public static MeasurementRowView From(BodyMeasurement measurement, string unit, bool isDark)
    {
        var displayWaist = unit == "in" ? measurement.WaistCm / 2.54 : measurement.WaistCm;
        var displayHips = unit == "in" ? measurement.HipsCm / 2.54 : measurement.HipsCm;
        var ratio = WaistHipsCalculator.Ratio(measurement.WaistCm, measurement.HipsCm);

        return new MeasurementRowView
        {
            Measurement = measurement,
            DayNumber = measurement.DateTime.Day.ToString(),
            MonthAbbrev = measurement.DateTime.ToString("MMM").ToUpperInvariant(),
            WaistHipsText = $"{displayWaist:0.0} / {displayHips:0.0} {unit}",
            NoteText = string.IsNullOrWhiteSpace(measurement.Note) ? string.Empty : $"\"{measurement.Note}\"",
            RatioText = ratio.ToString("0.00"),
            RatioColor = WaistHipsCalculator.GetCategoryColor(ratio, isDark)
        };
    }
}
