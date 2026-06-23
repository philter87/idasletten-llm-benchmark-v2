using Microsoft.Data.Sqlite;

namespace Idasletten.Shared;

public sealed class SqliteConnectionHolder : IDisposable
{
    public SqliteConnection Connection { get; }
    public SqliteConnectionHolder(string connectionString)
    {
        Connection = new SqliteConnection(connectionString);
        Connection.Open();
    }
    /// <summary>Used by tests to share an already-opened in-memory connection.</summary>
    public SqliteConnectionHolder(SqliteConnection existing) => Connection = existing;
    public void Dispose() => Connection.Dispose();
}