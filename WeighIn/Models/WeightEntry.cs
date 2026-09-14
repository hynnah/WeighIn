using SQLite;

namespace WeighIn.Models;

public sealed class WeightEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime DateTime { get; set; }

    public double WeightKg { get; set; }

    public double HeightCmAtEntry { get; set; }

    public string? Note { get; set; }
}
