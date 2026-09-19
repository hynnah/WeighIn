using SQLite;

namespace WeighIn.Models;

public sealed class BodyMeasurement
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime DateTime { get; set; }

    public double WaistCm { get; set; }

    public double HipsCm { get; set; }

    public string? Note { get; set; }
}
