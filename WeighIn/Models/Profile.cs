using SQLite;

namespace WeighIn.Models;

public sealed class Profile
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public double HeightCm { get; set; } = 170;

    public string WeightUnitPreference { get; set; } = "kg";

    public string BmiStandard { get; set; } = "Asian";

    public double? TargetWeightKg { get; set; }

    public DateTime? TargetDate { get; set; }
}
