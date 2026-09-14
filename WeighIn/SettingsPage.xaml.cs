using System.Globalization;
using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn;

public partial class SettingsPage : ContentPage
{
    private readonly AppDatabase database = new();
    private Profile? profile;

    public SettingsPage()
    {
        InitializeComponent();
        WeightUnitPicker.ItemsSource = new[] { "kg", "lb" };
        BmiStandardPicker.ItemsSource = new[] { "Asian", "General / WHO" };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        profile = await database.GetProfileAsync();
        HeightEntry.Text = profile.HeightCm.ToString("0.##", CultureInfo.CurrentCulture);
        WeightUnitPicker.SelectedItem = profile.WeightUnitPreference;
        BmiStandardPicker.SelectedItem = profile.BmiStandard == "General" ? "General / WHO" : "Asian";
        TargetWeightEntry.Text = profile.TargetWeightKg?.ToString("0.##", CultureInfo.CurrentCulture);
        TargetDatePicker.Date = profile.TargetDate ?? DateTime.Today;
        UpdateBmiPreview();
    }

    private void OnProfileInputChanged(object? sender, EventArgs e) => UpdateBmiPreview();

    private void UpdateBmiPreview()
    {
        if (!double.TryParse(HeightEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var heightCm) || heightCm <= 0)
        {
            BmiPreviewLabel.Text = "Enter your height to preview BMI.";
            return;
        }

        var bmi = 71.6 / Math.Pow(heightCm / 100, 2);
        var standard = BmiStandardPicker.SelectedItem?.ToString() == "General / WHO" ? "General" : "Asian";
        var category = standard == "Asian"
            ? bmi < 18.5 ? "Underweight" : bmi < 23 ? "Normal" : bmi < 25 ? "Overweight" : "Obese"
            : bmi < 18.5 ? "Underweight" : bmi < 25 ? "Normal" : bmi < 30 ? "Overweight" : "Obese";
        BmiPreviewLabel.Text = $"Preview at 71.6 kg: BMI {bmi:0.0} · {category}";
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (profile is null)
            return;

        if (!double.TryParse(HeightEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var heightCm) || heightCm is < 50 or > 250)
        {
            await DisplayAlertAsync("Check height", "Enter a height between 50 and 250 cm.", "Done");
            return;
        }

        double? targetWeightKg = null;
        if (!string.IsNullOrWhiteSpace(TargetWeightEntry.Text))
        {
            if (!double.TryParse(TargetWeightEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var target) || target is < 20 or > 500)
            {
                await DisplayAlertAsync("Check goal", "Enter a target weight between 20 and 500 kg, or leave it blank.", "Done");
                return;
            }

            targetWeightKg = target;
        }

        profile.HeightCm = heightCm;
        profile.WeightUnitPreference = WeightUnitPicker.SelectedItem?.ToString() ?? "kg";
        profile.BmiStandard = BmiStandardPicker.SelectedItem?.ToString() == "General / WHO" ? "General" : "Asian";
        profile.TargetWeightKg = targetWeightKg;
        profile.TargetDate = targetWeightKg.HasValue ? TargetDatePicker.Date : null;
        await database.SaveProfileAsync(profile);
        await DisplayAlertAsync("Saved", "Your profile has been saved on this device.", "Done");
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
