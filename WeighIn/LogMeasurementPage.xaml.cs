using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn;

public partial class LogMeasurementPage : ContentPage
{
    private readonly AppDatabase database = new();
    private readonly BodyMeasurement? existing;
    private string unit = "cm";

    public LogMeasurementPage(BodyMeasurement? existing = null)
    {
        InitializeComponent();
        this.existing = existing;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var profile = await database.GetProfileAsync();
        unit = profile.WeightUnitPreference == "lb" ? "in" : "cm";
        WaistUnitLabel.Text = unit;
        HipsUnitLabel.Text = unit;

        if (existing is not null)
        {
            EyebrowLabel.Text = "EDIT MEASUREMENT";
            DateLabel.Text = existing.DateTime.ToString("ddd, dd MMM yyyy");
            WaistEntry.Text = ToDisplay(existing.WaistCm).ToString("0.0");
            HipsEntry.Text = ToDisplay(existing.HipsCm).ToString("0.0");
            NoteEntry.Text = existing.Note;
            PrimaryActionButton.Text = "Save changes";
            DeleteButton.IsVisible = true;
        }
        else
        {
            DateLabel.Text = "Today";
        }

        RefreshRatioPreview();
    }

    private double ToDisplay(double cm) => unit == "in" ? cm / 2.54 : cm;
    private double ToCm(double display) => unit == "in" ? display * 2.54 : display;

    private void OnMeasurementChanged(object? sender, TextChangedEventArgs e) => RefreshRatioPreview();

    private void RefreshRatioPreview()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        if (!double.TryParse(WaistEntry.Text, out var waist) || !double.TryParse(HipsEntry.Text, out var hips) || waist <= 0 || hips <= 0)
        {
            RatioPreviewLabel.Text = string.Empty;
            return;
        }

        var ratio = WaistHipsCalculator.Ratio(ToCm(waist), ToCm(hips));
        var category = WaistHipsCalculator.GetCategory(ratio);
        RatioPreviewLabel.Text = $"Ratio {ratio:0.00} · {category}";
        RatioPreviewLabel.TextColor = WaistHipsCalculator.GetCategoryColor(ratio, isDark);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!double.TryParse(WaistEntry.Text, out var waist) || waist <= 0)
        {
            await DisplayAlertAsync("Enter a waist measurement", "Type a waist measurement before saving.", "Done");
            return;
        }

        if (!double.TryParse(HipsEntry.Text, out var hips) || hips <= 0)
        {
            await DisplayAlertAsync("Enter a hip measurement", "Type a hip measurement before saving.", "Done");
            return;
        }

        var measurement = existing ?? new BodyMeasurement { DateTime = DateTime.Now };
        measurement.WaistCm = ToCm(waist);
        measurement.HipsCm = ToCm(hips);
        measurement.Note = string.IsNullOrWhiteSpace(NoteEntry.Text) ? null : NoteEntry.Text.Trim();

        await database.SaveBodyMeasurementAsync(measurement);
        await Navigation.PopModalAsync();
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (existing is null)
            return;

        var confirmed = await DisplayAlertAsync("Delete measurement", $"Remove the measurement from {existing.DateTime:dd MMM yyyy}?", "Delete", "Cancel");
        if (!confirmed)
            return;

        await database.DeleteBodyMeasurementAsync(existing);
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
