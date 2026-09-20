using Microsoft.Maui.Controls.Shapes;
using WeighIn.Services;

namespace WeighIn;

public partial class AppLockSettingsPage : ContentPage
{
    private readonly AppDatabase database = new();
    private string? existingPin;
    private string? finalPin;

    private bool confirmingPin;
    private string pinBuffer = string.Empty;
    private string confirmBuffer = string.Empty;

    public AppLockSettingsPage()
    {
        InitializeComponent();
        BuildPinKeypad();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var profile = await database.GetProfileAsync();
        existingPin = profile.Pin;
        finalPin = null;
        LockSwitch.IsToggled = profile.LockEnabled;
        RefreshVisibility();
    }

    private void OnLockToggled(object? sender, ToggledEventArgs e)
    {
        if (LockSwitch.IsToggled && existingPin is null)
            ShowPinEntry();
        else if (!LockSwitch.IsToggled)
            PinEntrySection.IsVisible = false;

        RefreshVisibility();
    }

    private void OnChangePinTapped(object? sender, EventArgs e) => ShowPinEntry();

    private void ShowPinEntry()
    {
        confirmingPin = false;
        pinBuffer = string.Empty;
        confirmBuffer = string.Empty;
        finalPin = null;
        PinInstructionLabel.Text = "Choose a 4-digit PIN.";
        PinMismatchLabel.Text = string.Empty;
        RefreshPinDots();
        PinEntrySection.IsVisible = true;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        ChangePinLabel.IsVisible = LockSwitch.IsToggled && existingPin is not null && !PinEntrySection.IsVisible;
    }

    private void BuildPinKeypad()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var keyBackground = isDark ? Color.FromArgb("#232532") : Color.FromArgb("#EDEAE2");
        var keyText = isDark ? Color.FromArgb("#E9E9ED") : Color.FromArgb("#182C2B");
        var delText = isDark ? Color.FromArgb("#9397AB") : Color.FromArgb("#8D8A82");

        PinKeypadGrid.Children.Clear();
        string[] keys = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "", "0", "del"];
        for (var index = 0; index < keys.Length; index++)
        {
            var key = keys[index];
            if (key == string.Empty)
                continue;

            var button = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                BackgroundColor = keyBackground,
                HeightRequest = 48,
                Content = new Label
                {
                    Text = key == "del" ? "⌫" : key,
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = key == "del" ? delText : keyText
                }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OnPinKeyTapped(key);
            button.GestureRecognizers.Add(tap);
            Grid.SetRow(button, index / 3);
            Grid.SetColumn(button, index % 3);
            PinKeypadGrid.Children.Add(button);
        }
    }

    private void OnPinKeyTapped(string key)
    {
        ref var buffer = ref (confirmingPin ? ref confirmBuffer : ref pinBuffer);

        if (key == "del")
        {
            if (buffer.Length > 0)
                buffer = buffer[..^1];
            RefreshPinDots();
            return;
        }

        if (buffer.Length >= 4)
            return;

        buffer += key;
        RefreshPinDots();

        if (buffer.Length == 4)
        {
            if (!confirmingPin)
            {
                confirmingPin = true;
                PinInstructionLabel.Text = "Confirm your PIN.";
            }
            else if (confirmBuffer == pinBuffer)
            {
                finalPin = pinBuffer;
                PinMismatchLabel.Text = string.Empty;
            }
            else
            {
                PinMismatchLabel.Text = "Those do not match. Backspace and try again.";
                confirmBuffer = string.Empty;
                RefreshPinDots();
            }
        }
    }

    private void RefreshPinDots()
    {
        BuildDots(PinDotsStack, pinBuffer.Length);
        BuildDots(ConfirmDotsStack, confirmBuffer.Length);
    }

    private static void BuildDots(Layout stack, int filledCount)
    {
        stack.Children.Clear();
        for (var index = 0; index < 4; index++)
        {
            stack.Children.Add(new BoxView
            {
                WidthRequest = 10,
                HeightRequest = 10,
                CornerRadius = 5,
                Color = index < filledCount ? Color.FromArgb("#9184D9") : Color.FromArgb("#8D8A82")
            });
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var profile = await database.GetProfileAsync();

        if (!LockSwitch.IsToggled)
        {
            profile.LockEnabled = false;
            profile.Pin = null;
        }
        else
        {
            var pinToSave = finalPin ?? existingPin;
            if (pinToSave is null)
            {
                await DisplayAlertAsync("Set a PIN", "Choose a 4-digit PIN before saving.", "OK");
                return;
            }

            profile.LockEnabled = true;
            profile.Pin = pinToSave;
        }

        await database.SaveProfileAsync(profile);
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
