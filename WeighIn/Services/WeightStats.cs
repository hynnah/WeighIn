namespace WeighIn.Services;

public static class WeightStats
{
    public static int ComputeStreak(IReadOnlyList<DailySummary> summariesDescending)
    {
        var loggedDays = summariesDescending.Select(summary => summary.Date).ToHashSet();
        var streak = 0;
        var cursor = DateTime.Today;
        while (loggedDays.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    public static double? DeltaSinceLast(IReadOnlyList<DailySummary> summariesDescending)
    {
        if (summariesDescending.Count < 2)
            return null;

        return Math.Round(summariesDescending[0].AverageWeightKg - summariesDescending[1].AverageWeightKg, 1);
    }

    public static double? DeltaSinceStart(IReadOnlyList<DailySummary> summariesDescending)
    {
        if (summariesDescending.Count < 2)
            return null;

        return Math.Round(summariesDescending[0].AverageWeightKg - summariesDescending[^1].AverageWeightKg, 1);
    }

    public static double WeeklyRateKg(IReadOnlyList<DailySummary> series)
    {
        if (series.Count < 4)
            return 0;

        var ascending = series.OrderBy(summary => summary.Date).ToList();
        var n = ascending.Count;
        double sx = 0, sy = 0, sxy = 0, sxx = 0;
        var firstDate = ascending[0].Date;

        foreach (var point in ascending)
        {
            var x = (point.Date - firstDate).TotalDays;
            var y = point.AverageWeightKg;
            sx += x;
            sy += y;
            sxy += x * y;
            sxx += x * x;
        }

        var denominator = n * sxx - sx * sx;
        if (denominator == 0)
            return 0;

        var slopePerDay = (n * sxy - sx * sy) / denominator;
        return slopePerDay * 7;
    }

    public static List<(DateTime Date, double Average)> MovingAverage(IReadOnlyList<DailySummary> summariesDescending, int windowDays = 7)
    {
        var ascending = summariesDescending.OrderBy(summary => summary.Date).ToList();
        var result = new List<(DateTime, double)>();

        for (var index = 0; index < ascending.Count; index++)
        {
            var windowStart = ascending[index].Date.AddDays(-(windowDays - 1));
            var window = ascending.Where(summary => summary.Date >= windowStart && summary.Date <= ascending[index].Date).ToList();
            result.Add((ascending[index].Date, Math.Round(window.Average(summary => summary.AverageWeightKg), 1)));
        }

        return result;
    }
}
