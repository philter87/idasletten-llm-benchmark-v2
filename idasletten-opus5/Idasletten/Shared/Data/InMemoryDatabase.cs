using Microsoft.Data.Sqlite;

namespace Idasletten.Shared.Data;

public static class InMemoryDatabase
{
    /// <summary>A named shared in-memory database - the name keeps parallel tests apart.</summary>
    public static string ConnectionString(string name) =>
        $"Data Source={name};Mode=Memory;Cache=Shared";

    public static bool IsInMemory(string? connectionString) =>
        connectionString?.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// The database the app runs on. Without a configured connection string - locally and in tests -
    /// it is a SQLite in-memory database. It is resolved from IConfiguration at resolve time and not
    /// while services are registered, so test hosts really do get their own database.
    /// </summary>
    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("Idasletten");

        return string.IsNullOrWhiteSpace(configured) ? ConnectionString("Idasletten") : configured;
    }
}

/// <summary>
/// A shared-cache in-memory database only lives as long as at least one connection to it is open, so
/// this singleton holds one open for the lifetime of the application. Migrations and seeding then work
/// exactly as they do against a file. It does nothing when the app runs on a file database.
/// </summary>
public sealed class InMemoryDatabaseKeepAlive(IConfiguration configuration) : IDisposable
{
    private SqliteConnection? connection;

    public void Open()
    {
        var connectionString = InMemoryDatabase.ResolveConnectionString(configuration);
        if (!InMemoryDatabase.IsInMemory(connectionString) || connection is not null)
        {
            return;
        }

        connection = new SqliteConnection(connectionString);
        connection.Open();
    }

    public void Dispose() => connection?.Dispose();
}
