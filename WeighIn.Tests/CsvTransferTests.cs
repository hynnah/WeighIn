using WeighIn.Models;
using WeighIn.Services;

namespace WeighIn.Tests;

public class CsvTransferTests
{
    [Fact]
    public void Export_ThenImport_RoundTripsEntries()
    {
        var entries = new List<WeightEntry>
        {
            new() { DateTime = new DateTime(2026, 1, 5, 7, 30, 0), WeightKg = 68.4, HeightCmAtEntry = 170, Note = "morning" },
            new() { DateTime = new DateTime(2026, 1, 6, 7, 15, 0), WeightKg = 68.1, HeightCmAtEntry = 170, Note = null }
        };

        var csv = CsvTransfer.Export(entries);
        var result = CsvTransfer.Import(csv, fallbackHeightCm: 170);

        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(68.4, result.Entries[0].WeightKg);
        Assert.Equal("morning", result.Entries[0].Note);
        Assert.Null(result.Entries[1].Note);
    }

    [Fact]
    public void Export_NoteWithCommaAndQuote_IsEscapedAndRestored()
    {
        var entries = new List<WeightEntry>
        {
            new() { DateTime = new DateTime(2026, 1, 5, 7, 0, 0), WeightKg = 70, HeightCmAtEntry = 170, Note = "felt \"heavy\", tired" }
        };

        var csv = CsvTransfer.Export(entries);
        var result = CsvTransfer.Import(csv, fallbackHeightCm: 170);

        Assert.Equal("felt \"heavy\", tired", result.Entries[0].Note);
    }

    [Fact]
    public void Import_MissingHeight_FallsBackToProvidedHeight()
    {
        var csv = "Date,Time,WeightKg,HeightCm,Note\n2026-01-05,07:00,68.0,,\n";
        var result = CsvTransfer.Import(csv, fallbackHeightCm: 165);

        Assert.Single(result.Entries);
        Assert.Equal(165, result.Entries[0].HeightCmAtEntry);
    }

    [Theory]
    [InlineData("Date,Time,WeightKg,HeightCm,Note\n2026-01-05,07:00,19.9,170,\n")]
    [InlineData("Date,Time,WeightKg,HeightCm,Note\n2026-01-05,07:00,400.1,170,\n")]
    [InlineData("Date,Time,WeightKg,HeightCm,Note\n2026-01-05,07:00,notanumber,170,\n")]
    [InlineData("Date,Time,WeightKg,HeightCm,Note\nnotadate,07:00,68.0,170,\n")]
    public void Import_InvalidRow_IsSkippedRatherThanThrowing(string csv)
    {
        var result = CsvTransfer.Import(csv, fallbackHeightCm: 170);

        Assert.Empty(result.Entries);
        Assert.Equal(1, result.SkippedCount);
    }

    [Fact]
    public void Import_BoundaryWeights_AreAccepted()
    {
        var csv = "Date,Time,WeightKg,HeightCm,Note\n2026-01-05,07:00,20,170,\n2026-01-06,07:00,400,170,\n";
        var result = CsvTransfer.Import(csv, fallbackHeightCm: 170);

        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(2, result.Entries.Count);
    }
}
