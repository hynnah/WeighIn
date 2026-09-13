namespace WeighIn;

public partial class LockPage : ContentPage
{
    private const string DemoPin = "1234";
    private string enteredPin = string.Empty;

    public LockPage()
    {
        InitializeComponent();
    }

    private async void OnDigitClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || enteredPin.Length >= DemoPin.Length)
            return;

        enteredPin += button.CommandParameter?.ToString();
        UpdateDots();

        if (enteredPin.Length == DemoPin.Length)
        {
            if (enteredPin == DemoPin)
            {
                Application.Current!.Windows[0].Page = new MainPage();
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
            dots[index].Color = index < enteredPin.Length ? Color.FromArgb("#E29A5F") : Color.FromArgb("#B9C9BC");
    }
}