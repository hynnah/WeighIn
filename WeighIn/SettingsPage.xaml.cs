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
        NameEntry.Text = profile.Name;
        HeightEntry.Text = profile.HeightCm.ToString("0.##", CultureInfo.CurrentCulture);
        WeightUnitPicker.SelectedItem = profile.WeightUnitPreference;
        BmiStandardPicker.SelectedItem = profile.BmiStandard == "General" ? "General / WHO" : "Asian";
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

        profile.Name = string.IsNullOrWhiteSpace(NameEntry.Text) ? null : NameEntry.Text.Trim();
        profile.HeightCm = heightCm;
        profile.WeightUnitPreference = WeightUnitPicker.SelectedItem?.ToString() ?? "kg";
        profile.BmiStandard = BmiStandardPicker.SelectedItem?.ToString() == "General / WHO" ? "General" : "Asian";
        await database.SaveProfileAsync(profile);
        await DisplayAlertAsync("Saved", "Your profile has been saved on this device.", "Done");
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
