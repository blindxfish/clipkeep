using ClipForge.Infrastructure.Storage;
using Microsoft.Data.Sqlite;

namespace ClipForge.Infrastructure.Database;

/// <summary>
/// Owns the SQLite connection string and schema initialization. Callers open a
/// fresh connection per unit of work via <see cref="OpenConnection"/>.
/// </summary>
public sealed class ClipDatabase
{
    private readonly string _connectionString;

    public ClipDatabase(AppPaths paths)
    {
        paths.EnsureCreated();
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = paths.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <summary>Create tables, indexes, FTS table and triggers if absent.</summary>
    public void Initialize()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SchemaSql.Schema;
        cmd.ExecuteNonQuery();
    }
}
