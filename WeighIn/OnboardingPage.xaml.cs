using Microsoft.Maui.Controls.Shapes;
using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn;

public partial class OnboardingPage : ContentPage
{
    private static readonly string[] StepLabels = ["WELCOME", "ABOUT YOU", "TODAY'S WEIGHT", "YOUR GOAL", "REMINDER", "LOCK THE APP"];
    private static readonly int[] WeekOptions = [8, 12, 16, 24];
    private const int TotalSteps = 6;

    private readonly AppDatabase database = new();
    private Profile profile = new();

    private int step;
    private string name = string.Empty;
    private double heightCm = 170;
    private string unit = "kg";
    private string standard = "Asian";
    private double? todayWeightKg;
    private double goalKg = 68;
    private DateTime goalTargetDate = DateTime.Today.AddDays(WeekOptions[1] * 7);
    private string? selectedReminderTime = "07:00";

    private bool skipWeight;
    private bool skipGoal;
    private bool skipReminder;
    private bool skipLock;

    private bool confirmingPin;
    private string pinBuffer = string.Empty;
    private string confirmBuffer = string.Empty;
    private string? finalPin;

    public OnboardingPage()
    {
        InitializeComponent();
        BuildProgressBars();
        BuildWeeksOptions();
        BuildPinKeypad();
        GoalDatePicker.MinimumDate = DateTime.Today;
        ReminderTimePicker.Time = TimeSpan.Parse(selectedReminderTime!);
        RefreshReminderTimeDisplayLabel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        profile = await database.GetProfileAsync();
        name = profile.Name ?? string.Empty;
        NameEntry.Text = name;
        heightCm = profile.HeightCm;
        unit = profile.WeightUnitPreference == "lb" ? "lb" : "kg";
        standard = profile.BmiStandard == "General" ? "General" : "Asian";
        ShowStep(0);
    }

    private double ToDisplayWeight(double kg) => unit == "lb" ? kg / 0.45359237 : kg;
    private double ToKg(double display) => unit == "lb" ? display * 0.45359237 : display;
    private double StepKg => unit == "lb" ? 0.1 * 0.45359237 : 0.1;

    private void BuildProgressBars()
    {
        ProgressBarsStack.Children.Clear();
        for (var index = 0; index < TotalSteps; index++)
        {
            ProgressBarsStack.Children.Add(new BoxView
            {
                HeightRequest = 3,
                WidthRequest = 40,
                CornerRadius = 2,
                Color = Color.FromArgb("#2C2F3D")
            });
        }
    }

    private void ShowStep(int newStep)
    {
        step = newStep;

        Step0Panel.IsVisible = step == 0;
        Step1Panel.IsVisible = step == 1;
        Step2Panel.IsVisible = step == 2;
        Step3Panel.IsVisible = step == 3;
        Step4Panel.IsVisible = step == 4;
        Step5Panel.IsVisible = step == 5;

        for (var index = 0; index < ProgressBarsStack.Children.Count; index++)
        {
            if (ProgressBarsStack.Children[index] is BoxView bar)
                bar.Color = index <= step ? Color.FromArgb("#9184D9") : Color.FromArgb("#2C2F3D");
        }

        StepLabel.Text = StepLabels[step];
        BackLabel.IsVisible = step > 0;
        SkipLabel.IsVisible = step is 2 or 3 or 4 or 5;
        SkipLabel.Text = step == 5 ? "Skip — no lock" : "Skip for now";

        switch (step)
        {
            case 0:
                NextButton.Text = "Get started";
                break;
            case 1:
                NextButton.Text = "Continue";
                RefreshHeightDisplay();
                RefreshUnitDisplay();
                RefreshStandardDisplay();
                break;
            case 2:
                NextButton.Text = "Continue";
                TodayWeightUnitLabel.Text = unit;
                RefreshBmiPreview();
                break;
            case 3:
                NextButton.Text = "Continue";
                InitializeGoalDefaultIfNeeded();
                GoalUnitLabel.Text = unit;
                RefreshGoalDisplay();
                break;
            case 4:
                NextButton.Text = "Continue";
                ReminderSwitch.IsToggled = true;
                RefreshReminderTimesDisplay();
                break;
            case 5:
                confirmingPin = false;
                pinBuffer = string.Empty;
                confirmBuffer = string.Empty;
                NextButton.Text = "Finish";
                RefreshPinDots();
                PinInstructionLabel.Text = "Choose a 4-digit PIN.";
                PinMismatchLabel.Text = string.Empty;
                break;
        }
    }

    private void OnNameChanged(object? sender, TextChangedEventArgs e) => name = e.NewTextValue ?? string.Empty;

    private void RefreshHeightDisplay() => HeightValueLabel.Text = heightCm.ToString("0");

    private void OnHeightDownTapped(object? sender, EventArgs e)
    {
        heightCm = Math.Max(100, heightCm - 1);
        RefreshHeightDisplay();
    }

    private void OnHeightUpTapped(object? sender, EventArgs e)
    {
        heightCm = Math.Min(230, heightCm + 1);
        RefreshHeightDisplay();
    }

    private void RefreshUnitDisplay()
    {
        var activeBg = Color.FromArgb("#3A3266");
        var activeFg = Color.FromArgb("#D8D2FF");
        var inactiveBg = Color.FromArgb("#1D1F2E");
        var inactiveFg = Color.FromArgb("#9397AB");

        KgUnitBorder.BackgroundColor = unit == "kg" ? activeBg : inactiveBg;
        KgUnitLabel.TextColor = unit == "kg" ? activeFg : inactiveFg;
        LbUnitBorder.BackgroundColor = unit == "lb" ? activeBg : inactiveBg;
        LbUnitLabel.TextColor = unit == "lb" ? activeFg : inactiveFg;
    }

    private void OnKgUnitTapped(object? sender, EventArgs e)
    {
        unit = "kg";
        RefreshUnitDisplay();
    }

    private void OnLbUnitTapped(object? sender, EventArgs e)
    {
        unit = "lb";
        RefreshUnitDisplay();
    }

    private void RefreshStandardDisplay()
    {
        var activeBg = Color.FromArgb("#3A3266");
        var activeFg = Color.FromArgb("#D8D2FF");
        var inactiveBg = Color.FromArgb("#1D1F2E");
        var inactiveFg = Color.FromArgb("#E9E9ED");

        AsianStdBorder.BackgroundColor = standard == "Asian" ? activeBg : inactiveBg;
        AsianStdLabel.TextColor = standard == "Asian" ? activeFg : inactiveFg;
        GeneralStdBorder.BackgroundColor = standard == "General" ? activeBg : inactiveBg;
        GeneralStdLabel.TextColor = standard == "General" ? activeFg : inactiveFg;
    }

    private void OnAsianStandardTapped(object? sender, EventArgs e)
    {
        standard = "Asian";
        RefreshStandardDisplay();
    }

    private void OnGeneralStandardTapped(object? sender, EventArgs e)
    {
        standard = "General";
        RefreshStandardDisplay();
    }

    private void OnTodayWeightChanged(object? sender, TextChangedEventArgs e)
    {
        todayWeightKg = double.TryParse(e.NewTextValue, out var value) && value > 0 ? ToKg(value) : null;
        RefreshBmiPreview();
    }

    private void RefreshBmiPreview()
    {
        if (todayWeightKg is not { } kg)
        {
            TodayBmiLabel.Text = string.Empty;
            return;
        }

        var bmi = kg / Math.Pow(heightCm / 100, 2);
        var category = BmiCalculator.GetCategory(bmi, standard == "General" ? "General" : "Asian");
        TodayBmiLabel.Text = $"BMI {bmi:0.0} · {category}";
        TodayBmiLabel.TextColor = BmiCalculator.GetCategoryColor(bmi, standard == "General" ? "General" : "Asian", true);
    }

    private void InitializeGoalDefaultIfNeeded()
    {
        if (goalKg > 0 && Math.Abs(goalKg - 68) > 0.01)
            return;

        var baseKg = todayWeightKg ?? 70;
        goalKg = Math.Round(Math.Max(30, baseKg - 2), 1);
    }

    private void RefreshGoalDisplay()
    {
        GoalValueLabel.Text = ToDisplayWeight(goalKg).ToString("0.0");
        RefreshWeeksOptionsDisplay();
        RefreshGoalRate();
    }

    private void OnGoalDownTapped(object? sender, EventArgs e)
    {
        goalKg = Math.Max(30, Math.Round((goalKg - StepKg) * 10) / 10);
        RefreshGoalDisplay();
    }

    private void OnGoalUpTapped(object? sender, EventArgs e)
    {
        goalKg = Math.Round((goalKg + StepKg) * 10) / 10;
        RefreshGoalDisplay();
    }

    private void BuildWeeksOptions()
    {
        WeeksOptionsGrid.Children.Clear();
        for (var index = 0; index < WeekOptions.Length; index++)
        {
            var weeks = WeekOptions[index];
            var date = DateTime.Today.AddDays(weeks * 7);
            var chip = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(10, 9),
                BindingContext = weeks,
                Content = new VerticalStackLayout
                {
                    Spacing = 1,
                    Children =
                    {
                        new Label { Text = date.ToString("d MMM"), FontSize = 14, TextColor = Colors.White },
                        new Label { Text = $"{weeks}w", FontSize = 11, Opacity = 0.7, TextColor = Colors.White }
                    }
                }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                goalTargetDate = date;
                RefreshGoalDisplay();
            };
            chip.GestureRecognizers.Add(tap);
            Grid.SetRow(chip, index / 2);
            Grid.SetColumn(chip, index % 2);
            WeeksOptionsGrid.Children.Add(chip);
        }
    }

    private void RefreshWeeksOptionsDisplay()
    {
        foreach (var child in WeeksOptionsGrid.Children)
        {
            if (child is not Border { BindingContext: int weeks } chip || chip.Content is not VerticalStackLayout stack)
                continue;

            var isActive = goalTargetDate.Date == DateTime.Today.AddDays(weeks * 7).Date;
            chip.BackgroundColor = isActive ? Color.FromArgb("#3A3266") : Color.FromArgb("#1D1F2E");
            var fg = isActive ? Color.FromArgb("#D8D2FF") : Color.FromArgb("#9397AB");
            foreach (var label in stack.Children.OfType<Label>())
                label.TextColor = fg;
        }

        if (GoalDatePicker.Date != goalTargetDate.Date)
            GoalDatePicker.Date = goalTargetDate.Date;
    }

    private void OnGoalDateSelected(object? sender, DateChangedEventArgs e)
    {
        var newDate = e.NewDate.GetValueOrDefault(goalTargetDate).Date;
        if (newDate == goalTargetDate.Date)
            return;

        goalTargetDate = newDate;
        RefreshGoalDisplay();
    }

    private void RefreshGoalRate()
    {
        var weeksOut = Math.Max(0, (int)Math.Round((goalTargetDate.Date - DateTime.Today).TotalDays / 7));

        if (weeksOut <= 0)
        {
            GoalRateLabel.Text = "Pick a date in the future.";
            return;
        }

        if (todayWeightKg is not { } currentKg)
        {
            GoalRateLabel.Text = $"Aiming for {ToDisplayWeight(goalKg):0.0} {unit} by {goalTargetDate:d MMM}.";
            return;
        }

        var diffKg = currentKg - goalKg;
        var rateKgPerWeek = diffKg / weeksOut;
        var rateDisplay = ToDisplayWeight(Math.Abs(rateKgPerWeek));
        GoalRateLabel.Text = diffKg <= 0
            ? $"You're already at or below this goal."
            : $"That's about {rateDisplay:0.00} {unit} a week to reach your goal.";
    }

    private void RefreshReminderTimesDisplay()
    {
        ReminderTimeRow.Opacity = ReminderSwitch.IsToggled ? 1.0 : 0.4;
        ReminderTimeRow.InputTransparent = !ReminderSwitch.IsToggled;
    }

    private void OnReminderToggled(object? sender, ToggledEventArgs e) => RefreshReminderTimesDisplay();

    private void OnReminderTimeSelected(object? sender, TimeChangedEventArgs e)
    {
        selectedReminderTime = e.NewTime.GetValueOrDefault().ToString(@"hh\:mm");
        RefreshReminderTimeDisplayLabel();
    }

    private void RefreshReminderTimeDisplayLabel()
    {
        var time = ReminderTimePicker.Time.GetValueOrDefault();
        ReminderTimeDisplayLabel.Text = DateTime.Today.Add(time).ToString("hh:mm tt");
    }

    private void BuildPinKeypad()
    {
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
                BackgroundColor = Color.FromArgb("#232532"),
                HeightRequest = 48,
                Content = new Label
                {
                    Text = key == "del" ? "⌫" : key,
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = key == "del" ? Color.FromArgb("#9397AB") : Color.FromArgb("#E9E9ED")
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
            else
            {
                if (confirmBuffer == pinBuffer)
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
    }

    private void RefreshPinDots()
    {
        BuildDots(PinDotsStack, pinBuffer.Length);
        BuildDots(ConfirmDotsStack, confirmBuffer.Length);
    }

    private static void BuildDots(Layout host, int filledCount)
    {
        host.Children.Clear();
        for (var index = 0; index < 4; index++)
        {
            host.Children.Add(new BoxView
            {
                WidthRequest = 11,
                HeightRequest = 11,
                CornerRadius = 6,
                Color = index < filledCount ? Color.FromArgb("#9184D9") : Color.FromArgb("#3F424D")
            });
        }
    }

    private void OnBackTapped(object? sender, EventArgs e)
    {
        if (step > 0)
            ShowStep(step - 1);
    }

    private void OnSkipTapped(object? sender, EventArgs e)
    {
        switch (step)
        {
            case 2:
                skipWeight = true;
                todayWeightKg = null;
                break;
            case 3:
                skipGoal = true;
                break;
            case 4:
                skipReminder = true;
                break;
            case 5:
                skipLock = true;
                _ = FinishAsync();
                return;
        }

        AdvanceStep();
    }

    private async void OnNextClicked(object? sender, EventArgs e)
    {
        if (step == 5)
        {
            if (!skipLock && finalPin is null)
                return;

            await FinishAsync();
            return;
        }

        AdvanceStep();
    }

    private void AdvanceStep()
    {
        if (step < TotalSteps - 1)
            ShowStep(step + 1);
    }

    private async Task FinishAsync()
    {
        profile.Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        profile.HeightCm = heightCm;
        profile.WeightUnitPreference = unit;
        profile.BmiStandard = standard == "General" ? "General" : "Asian";
        profile.HasOnboarded = true;

        if (!skipGoal)
        {
            profile.TargetWeightKg = goalKg;
            profile.TargetDate = goalTargetDate.Date;
        }

        if (!skipReminder)
        {
            profile.RemindersEnabled = ReminderSwitch.IsToggled;
            profile.ReminderTime = selectedReminderTime;
        }

        if (!skipLock && finalPin is not null)
        {
            profile.LockEnabled = true;
            profile.Pin = finalPin;
        }
        else
        {
            profile.LockEnabled = false;
            profile.Pin = null;
        }

        await database.SaveProfileAsync(profile);
        await ReminderScheduleHelper.ApplyAsync(profile);

        if (!skipWeight && todayWeightKg is { } weightKg)
        {
            await database.SaveEntryAsync(new WeightEntry
            {
                DateTime = DateTime.Now,
                WeightKg = weightKg,
                HeightCmAtEntry = heightCm,
                Note = null
            });
        }

        await Shell.Current.GoToAsync(profile.LockEnabled ? "//lock" : "//main/home");
    }
}
