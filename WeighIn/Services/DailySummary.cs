using WeighIn.Models;

namespace WeighIn.Services;

public sealed class DailySummary
{
    public required DateTime Date { get; init; }
    public required double AverageWeightKg { get; init; }
    public required double HeightCmAtEntry { get; init; }
    public string? Note { get; init; }
    public required IReadOnlyList<WeightEntry> Readings { get; init; }

    public static List<DailySummary> FromEntries(IEnumerable<WeightEntry> entries)
    {
        return entries
            .GroupBy(entry => entry.DateTime.Date)
            .Select(group =>
            {
                var readings = group.OrderBy(entry => entry.DateTime).ToList();
                var latest = readings[^1];
                return new DailySummary
                {
                    Date = group.Key,
                    AverageWeightKg = Math.Round(readings.Average(entry => entry.WeightKg), 1),
                    HeightCmAtEntry = latest.HeightCmAtEntry,
                    Note = readings.LastOrDefault(entry => !string.IsNullOrWhiteSpace(entry.Note))?.Note,
                    Readings = readings
                };
            })
            .OrderByDescending(summary => summary.Date)
            .ToList();
    }
}
