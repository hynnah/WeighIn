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
        isInitialized = true;
    }

    public async Task<Profile> GetProfileAsync()
    {
        await InitializeAsync();
        var profile = await connection.Table<Profile>().FirstOrDefaultAsync();
        if (profile is not null)
            return profile;

        profile = new Profile();
        await connection.InsertAsync(profile);
        return profile;
    }

    public async Task SaveProfileAsync(Profile profile)
    {
        await InitializeAsync();
        await connection.InsertOrReplaceAsync(profile);
    }
}
