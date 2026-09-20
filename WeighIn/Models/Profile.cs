using SQLite;

namespace WeighIn.Models;

public sealed class Profile
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public string? Name { get; set; }

    public double HeightCm { get; set; } = 170;

    public string WeightUnitPreference { get; set; } = "kg";

    public string BmiStandard { get; set; } = "Asian";

    public double? TargetWeightKg { get; set; }

    public DateTime? TargetDate { get; set; }

    public bool HasOnboarded { get; set; }

    public bool LockEnabled { get; set; }

    public string? Pin { get; set; }

    public bool RemindersEnabled { get; set; }

    public string? ReminderTime { get; set; }

    public string PreferredTheme { get; set; } = "System";
}
