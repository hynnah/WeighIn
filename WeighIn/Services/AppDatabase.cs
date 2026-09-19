using SQLite;
using WeighIn.Models;

namespace WeighIn.Services;

public sealed class AppDatabase
{
    private readonly SQLiteAsyncConnection connection;
    private bool isInitialized;

    public AppDatabase()
    {
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "weighin.db3");
        connection = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public async Task InitializeAsync()
    {
        if (isInitialized)
            return;

        await connection.CreateTableAsync<Profile>();
        await connection.CreateTableAsync<WeightEntry>();
        await connection.CreateTableAsync<BodyMeasurement>();
        isInitialized = true;
    }

    public async Task<Profile> GetProfileAsync()
    {
        await InitializeAsync();
        var profile = await connection.Table<Profile>().FirstOrDefaultAsync();
        if (profile is not null)
            return profile;

        profile = new Profile();
        try
        {
            await connection.InsertAsync(profile);
        }
        catch (SQLiteException)
        {
            // Another concurrent caller already inserted the default profile row.
        }

        return await connection.Table<Profile>().FirstOrDefaultAsync() ?? profile;
    }

    public async Task SaveProfileAsync(Profile profile)
    {
        await InitializeAsync();
        await connection.InsertOrReplaceAsync(profile);
    }

    public async Task<List<WeightEntry>> GetEntriesAsync()
    {
        await InitializeAsync();
        return await connection.Table<WeightEntry>()
            .OrderByDescending(entry => entry.DateTime)
            .ToListAsync();
    }

    public async Task<List<DailySummary>> GetDailySummariesAsync(double heightCm)
    {
        var entries = await GetEntriesWithDemoDataAsync(heightCm);
        return DailySummary.FromEntries(entries);
    }

    public async Task<List<WeightEntry>> GetEntriesWithDemoDataAsync(double heightCm)
    {
        var entries = await GetEntriesAsync();
        if (entries.Count > 0)
            return entries;

        var demoEntries = new[]
        {
            new WeightEntry { DateTime = DateTime.Today.AddDays(-2).AddHours(8), WeightKg = 53, HeightCmAtEntry = heightCm, Note = "Demo entry" },
            new WeightEntry { DateTime = DateTime.Today.AddDays(-1).AddHours(8), WeightKg = 52, HeightCmAtEntry = heightCm, Note = "Demo entry" },
            new WeightEntry { DateTime = DateTime.Today.AddHours(8), WeightKg = 51, HeightCmAtEntry = heightCm, Note = "Demo entry" }
        };

        foreach (var entry in demoEntries)
            await connection.InsertAsync(entry);

        return await GetEntriesAsync();
    }

    public async Task SaveEntryAsync(WeightEntry entry)
    {
        await InitializeAsync();
        if (entry.Id == 0)
            await connection.InsertAsync(entry);
        else
            await connection.UpdateAsync(entry);
    }

    public async Task DeleteEntryAsync(WeightEntry entry)
    {
        await InitializeAsync();
        await connection.DeleteAsync(entry);
    }

    public async Task<List<BodyMeasurement>> GetBodyMeasurementsAsync()
    {
        await InitializeAsync();
        return await connection.Table<BodyMeasurement>()
            .OrderByDescending(measurement => measurement.DateTime)
            .ToListAsync();
    }

    public async Task SaveBodyMeasurementAsync(BodyMeasurement measurement)
    {
        await InitializeAsync();
        if (measurement.Id == 0)
            await connection.InsertAsync(measurement);
        else
            await connection.UpdateAsync(measurement);
    }

    public async Task DeleteBodyMeasurementAsync(BodyMeasurement measurement)
    {
        await InitializeAsync();
        await connection.DeleteAsync(measurement);
    }
}
