using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn.Tests;

public class WeightStatsTests
{
    private static DailySummary Summary(DateTime date, double weightKg) => new()
    {
        Date = date.Date,
        AverageWeightKg = weightKg,
        HeightCmAtEntry = 170,
        Readings = [new WeightEntry { DateTime = date, WeightKg = weightKg, HeightCmAtEntry = 170 }]
    };

    [Fact]
    public void ComputeStreak_ConsecutiveDaysEndingToday_CountsAll()
    {
        var summaries = new List<DailySummary>
        {
            Summary(DateTime.Today, 70),
            Summary(DateTime.Today.AddDays(-1), 70.2),
            Summary(DateTime.Today.AddDays(-2), 70.4)
        };

        Assert.Equal(3, WeightStats.ComputeStreak(summaries));
    }

    [Fact]
    public void ComputeStreak_GapBeforeToday_StopsAtGap()
    {
        var summaries = new List<DailySummary>
        {
            Summary(DateTime.Today, 70),
            Summary(DateTime.Today.AddDays(-2), 70.4)
        };

        Assert.Equal(1, WeightStats.ComputeStreak(summaries));
    }

    [Fact]
    public void ComputeStreak_MissingToday_ReturnsZero()
    {
        var summaries = new List<DailySummary> { Summary(DateTime.Today.AddDays(-1), 70) };

        Assert.Equal(0, WeightStats.ComputeStreak(summaries));
    }

    [Fact]
    public void DeltaSinceLast_FewerThanTwoEntries_ReturnsNull()
    {
        var summaries = new List<DailySummary> { Summary(DateTime.Today, 70) };

        Assert.Null(WeightStats.DeltaSinceLast(summaries));
    }

    [Fact]
    public void DeltaSinceLast_ComparesFirstTwoDescendingEntries()
    {
        var summaries = new List<DailySummary>
        {
            Summary(DateTime.Today, 69.0),
            Summary(DateTime.Today.AddDays(-1), 70.0)
        };

        Assert.Equal(-1.0, WeightStats.DeltaSinceLast(summaries));
    }

    [Fact]
    public void DeltaSinceStart_ComparesFirstAndLastDescendingEntries()
    {
        var summaries = new List<DailySummary>
        {
            Summary(DateTime.Today, 68.0),
            Summary(DateTime.Today.AddDays(-1), 69.0),
            Summary(DateTime.Today.AddDays(-2), 70.0)
        };

        Assert.Equal(-2.0, WeightStats.DeltaSinceStart(summaries));
    }

    [Fact]
    public void WeeklyRateKg_FewerThanFourPoints_ReturnsZero()
    {
        var summaries = new List<DailySummary>
        {
            Summary(DateTime.Today, 70),
            Summary(DateTime.Today.AddDays(-1), 70.1),
            Summary(DateTime.Today.AddDays(-2), 70.2)
        };

        Assert.Equal(0, WeightStats.WeeklyRateKg(summaries));
    }

    [Fact]
    public void WeeklyRateKg_SteadyLoss_MatchesExpectedSlopeTimesSeven()
    {
        var summaries = Enumerable.Range(0, 10)
            .Select(day => Summary(DateTime.Today.AddDays(-day), 70 - 0.1 * (9 - day)))
            .ToList();

        var weeklyRate = WeightStats.WeeklyRateKg(summaries);

        Assert.Equal(-0.7, weeklyRate, precision: 3);
    }

    [Fact]
    public void MovingAverage_WindowCoveringAllPoints_AveragesEvenly()
    {
        var summaries = new List<DailySummary>
        {
            Summary(DateTime.Today.AddDays(-2), 70),
            Summary(DateTime.Today.AddDays(-1), 71),
            Summary(DateTime.Today, 72)
        };

        var result = WeightStats.MovingAverage(summaries, windowDays: 7);

        Assert.Equal(3, result.Count);
        Assert.Equal(71.0, result[^1].Average);
    }
}
