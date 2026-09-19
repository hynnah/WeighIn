using WeighIn.Services;

namespace WeighIn;

public partial class LockPage : ContentPage
{
    private readonly AppDatabase database = new();
    private string actualPin = string.Empty;
    private string enteredPin = string.Empty;

    public LockPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var profile = await database.GetProfileAsync();
        actualPin = profile.Pin ?? string.Empty;
        enteredPin = string.Empty;
        PinStatus.Text = string.Empty;
        UpdateDots();

#if ANDROID
        FingerprintLabel.IsVisible = Platforms.Android.BiometricAuthenticator.IsAvailable();
#endif
    }

    private async void OnFingerprintTapped(object? sender, EventArgs e)
    {
#if ANDROID
        var authenticated = await Platforms.Android.BiometricAuthenticator.AuthenticateAsync("Unlock WeighIn", "Confirm your fingerprint to continue");
        if (authenticated)
            await Shell.Current.GoToAsync("//main/home");
#else
        await Task.CompletedTask;
#endif
    }

    private async void OnDigitClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || enteredPin.Length >= 4)
            return;

        enteredPin += button.CommandParameter?.ToString();
        UpdateDots();

        if (enteredPin.Length == 4)
        {
            if (actualPin.Length == 4 && enteredPin == actualPin)
            {
                await Shell.Current.GoToAsync("//main/home");
                return;
            }

            PinStatus.Text = "That PIN did not match.";
            enteredPin = string.Empty;
            await Task.Delay(500);
            PinStatus.Text = string.Empty;
            UpdateDots();
        }
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (enteredPin.Length == 0)
            return;

        enteredPin = enteredPin[..^1];
        UpdateDots();
    }

    private void UpdateDots()
    {
        var dots = new[] { Dot1, Dot2, Dot3, Dot4 };
        for (var index = 0; index < dots.Length; index++)
            dots[index].Color = index < enteredPin.Length ? Color.FromArgb("#9184D9") : Color.FromArgb("#3F424D");
    }
}
