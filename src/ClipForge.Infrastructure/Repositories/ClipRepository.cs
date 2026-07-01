using ClipForge.Core.Models;
using ClipForge.Core.Storage;
using ClipForge.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace ClipForge.Infrastructure.Repositories;

/// <summary>
/// Raw-ADO.NET repository over SQLite. Dedup-on-write is enforced by the UNIQUE
/// content_hash column; search goes through the clipboard_fts index.
/// </summary>
public sealed class ClipRepository : IClipRepository
{
    private readonly ClipDatabase _db;

    public ClipRepository(ClipDatabase db) => _db = db;

    public (ClipEntry Entry, bool IsNew) Upsert(ClipEntry entry)
    {
        using var conn = _db.OpenConnection();

        var existing = FindByHash(conn, entry.ContentHash);
        if (existing is not null)
        {
            existing.CopyCount += 1;
            existing.LastCopiedAt = entry.LastCopiedAt;
            existing.UpdatedAt = entry.UpdatedAt;

            using var upd = conn.CreateCommand();
            upd.CommandText = """
                UPDATE clipboard_entries
                SET copy_count = $count, last_copied_at = $last, updated_at = $updated
                WHERE id = $id;
                """;
            upd.Parameters.AddWithValue("$count", existing.CopyCount);
            upd.Parameters.AddWithValue("$last", Iso(existing.LastCopiedAt));
            upd.Parameters.AddWithValue("$updated", Iso(existing.UpdatedAt));
            upd.Parameters.AddWithValue("$id", existing.Id);
            upd.ExecuteNonQuery();
            return (existing, false);
        }

        using var ins = conn.CreateCommand();
        ins.CommandText = """
            INSERT INTO clipboard_entries
                (type, content, content_hash, source_app, source_process, window_title,
                 favorite, copy_count, first_copied_at, last_copied_at, created_at, updated_at)
            VALUES
                ($type, $content, $hash, $app, $proc, $title,
                 $fav, $count, $first, $last, $created, $updated);
            SELECT last_insert_rowid();
            """;
        ins.Parameters.AddWithValue("$type", TypeToDb(entry.Type));
        ins.Parameters.AddWithValue("$content", (object?)entry.Content ?? DBNull.Value);
        ins.Parameters.AddWithValue("$hash", entry.ContentHash);
        ins.Parameters.AddWithValue("$app", (object?)entry.SourceApp ?? DBNull.Value);
        ins.Parameters.AddWithValue("$proc", (object?)entry.SourceProcess ?? DBNull.Value);
        ins.Parameters.AddWithValue("$title", (object?)entry.WindowTitle ?? DBNull.Value);
        ins.Parameters.AddWithValue("$fav", entry.Favorite ? 1 : 0);
        ins.Parameters.AddWithValue("$count", entry.CopyCount);
        ins.Parameters.AddWithValue("$first", Iso(entry.FirstCopiedAt));
        ins.Parameters.AddWithValue("$last", Iso(entry.LastCopiedAt));
        ins.Parameters.AddWithValue("$created", Iso(entry.CreatedAt));
        ins.Parameters.AddWithValue("$updated", Iso(entry.UpdatedAt));

        entry.Id = (long)ins.ExecuteScalar()!;
        return (entry, true);
    }

    public IReadOnlyList<ClipEntry> Query(ClipQuery query)
    {
        using var conn = _db.OpenConnection();
        using var cmd = conn.CreateCommand();

        var wheres = new List<string>();
        bool hasSearch = !string.IsNullOrWhiteSpace(query.Search);

        if (hasSearch)
        {
            // Join against the FTS index; MATCH ranks results.
            cmd.CommandText = """
                SELECT e.* FROM clipboard_entries e
                JOIN clipboard_fts f ON f.rowid = e.id
                WHERE clipboard_fts MATCH $match
                """;
            cmd.Parameters.AddWithValue("$match", BuildMatch(query.Search!));
        }
        else
        {
            cmd.CommandText = "SELECT e.* FROM clipboard_entries e WHERE 1=1";
        }

        if (query.Type is { } t)
        {
            wheres.Add("e.type = $type");
            cmd.Parameters.AddWithValue("$type", TypeToDb(t));
        }
        if (query.FavoritesOnly)
            wheres.Add("e.favorite = 1");

        if (wheres.Count > 0)
            cmd.CommandText += " AND " + string.Join(" AND ", wheres);

        cmd.CommandText += hasSearch
            ? " ORDER BY rank LIMIT $limit OFFSET $offset;"
            : " ORDER BY e.last_copied_at DESC LIMIT $limit OFFSET $offset;";
        cmd.Parameters.AddWithValue("$limit", query.Limit);
        cmd.Parameters.AddWithValue("$offset", query.Offset);

        var results = new List<ClipEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            results.Add(Map(reader));
        return results;
    }

    public ClipEntry? GetById(long id)
    {
        using var conn = _db.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM clipboard_entries WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public void SetFavorite(long id, bool favorite)
    {
        using var conn = _db.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE clipboard_entries SET favorite = $fav, updated_at = updated_at WHERE id = $id;";
        cmd.Parameters.AddWithValue("$fav", favorite ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var conn = _db.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM clipboard_entries WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static ClipEntry? FindByHash(SqliteConnection conn, string hash)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM clipboard_entries WHERE content_hash = $hash;";
        cmd.Parameters.AddWithValue("$hash", hash);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    /// <summary>
    /// Turn a user query into a safe FTS5 MATCH expression: each whitespace token
    /// becomes a prefix term, quoted to neutralize FTS operator characters.
    /// </summary>
    private static string BuildMatch(string search)
    {
        var tokens = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return "\"\"";
        return string.Join(" ", tokens.Select(tok =>
            "\"" + tok.Replace("\"", "\"\"") + "\"*"));
    }

    private static ClipEntry Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("id")),
        Type = TypeFromDb(r.GetString(r.GetOrdinal("type"))),
        Content = r.IsDBNull(r.GetOrdinal("content")) ? null : r.GetString(r.GetOrdinal("content")),
        ContentHash = r.GetString(r.GetOrdinal("content_hash")),
        SourceApp = Nullable(r, "source_app"),
        SourceProcess = Nullable(r, "source_process"),
        WindowTitle = Nullable(r, "window_title"),
        Favorite = r.GetInt64(r.GetOrdinal("favorite")) != 0,
        CopyCount = (int)r.GetInt64(r.GetOrdinal("copy_count")),
        FirstCopiedAt = ParseIso(r.GetString(r.GetOrdinal("first_copied_at"))),
        LastCopiedAt = ParseIso(r.GetString(r.GetOrdinal("last_copied_at"))),
        CreatedAt = ParseIso(r.GetString(r.GetOrdinal("created_at"))),
        UpdatedAt = ParseIso(r.GetString(r.GetOrdinal("updated_at"))),
    };

    private static string? Nullable(SqliteDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    private static string TypeToDb(ClipType t) => t.ToString().ToLowerInvariant();

    private static ClipType TypeFromDb(string s) =>
        Enum.TryParse<ClipType>(s, ignoreCase: true, out var t) ? t : ClipType.Text;

    private static string Iso(DateTimeOffset dt) => dt.ToUniversalTime().ToString("O");
    private static DateTimeOffset ParseIso(string s) => DateTimeOffset.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind);
}
