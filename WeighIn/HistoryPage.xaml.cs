using System.Globalization;
using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn;

public partial class HistoryPage : ContentPage
{
    private readonly AppDatabase database = new();

    public HistoryPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadEntriesAsync();
    }

    private async Task LoadEntriesAsync()
    {
        var profile = await database.GetProfileAsync();
        var entries = await database.GetEntriesWithDemoDataAsync(profile.HeightCm);
        EntriesView.ItemsSource = entries.Select(entry => new HistoryEntryView(entry, profile.WeightUnitPreference, profile.BmiStandard)).ToList();
    }

    private async void OnManageClicked(object? sender, EventArgs e)
    {
        if (EntriesView.SelectedItem is not HistoryEntryView selected)
        {
            await DisplayAlertAsync("Select an entry", "Tap an entry first, then choose manage.", "Done");
            return;
        }

        var action = await DisplayActionSheetAsync("Manage entry", "Cancel", null, "Edit weight", "Delete");
        if (action == "Delete")
        {
            await database.DeleteEntryAsync(selected.Entry);
        }
        else if (action == "Edit weight")
        {
            var value = await DisplayPromptAsync("Edit weight", "Weight in kg", "Save", "Cancel", selected.Entry.WeightKg.ToString("0.##", CultureInfo.CurrentCulture), keyboard: Keyboard.Numeric);
            if (double.TryParse(value, out var weightKg) && weightKg > 0)
                selected.Entry.WeightKg = weightKg;
            else
                return;
            await database.SaveEntryAsync(selected.Entry);
        }

        await LoadEntriesAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}

internal sealed class HistoryEntryView
{
    public HistoryEntryView(WeightEntry entry, string unit, string standard)
    {
        Entry = entry;
        DisplayDate = entry.DateTime.ToString("ddd, dd MMM yyyy · h:mm tt");
        var displayWeight = unit == "lb" ? entry.WeightKg / 0.45359237 : entry.WeightKg;
        DisplayWeight = $"{displayWeight:0.0} {unit}";
        var bmi = entry.WeightKg / Math.Pow(entry.HeightCmAtEntry / 100, 2);
        DisplayBmi = $"BMI {bmi:0.0} · {BmiCalculator.GetCategory(bmi, standard)}";
        Note = entry.Note ?? string.Empty;
    }

    public WeightEntry Entry { get; }
    public string DisplayDate { get; }
    public string DisplayWeight { get; }
    public string DisplayBmi { get; }
    public string Note { get; }
}
