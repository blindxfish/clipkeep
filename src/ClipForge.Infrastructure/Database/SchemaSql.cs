namespace ClipForge.Infrastructure.Database;

/// <summary>
/// Canonical DDL for the ClipForge database. The FTS5 table is external-content
/// (backed by clipboard_entries) and kept in sync via triggers — inserts/updates/
/// deletes on clipboard_entries must never bypass these triggers.
/// </summary>
internal static class SchemaSql
{
    public const string Schema = """
    PRAGMA journal_mode = WAL;
    PRAGMA foreign_keys = ON;

    CREATE TABLE IF NOT EXISTS clipboard_entries (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        type TEXT NOT NULL,
        content TEXT,
        content_hash TEXT NOT NULL UNIQUE,
        source_app TEXT,
        source_process TEXT,
        window_title TEXT,
        favorite INTEGER NOT NULL DEFAULT 0,
        copy_count INTEGER NOT NULL DEFAULT 1,
        first_copied_at TEXT NOT NULL,
        last_copied_at TEXT NOT NULL,
        created_at TEXT NOT NULL,
        updated_at TEXT NOT NULL
    );

    CREATE INDEX IF NOT EXISTS ix_entries_type ON clipboard_entries(type);
    CREATE INDEX IF NOT EXISTS ix_entries_last_copied ON clipboard_entries(last_copied_at);
    CREATE INDEX IF NOT EXISTS ix_entries_favorite ON clipboard_entries(favorite);

    CREATE TABLE IF NOT EXISTS urls (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        entry_id INTEGER NOT NULL,
        url TEXT NOT NULL,
        domain TEXT,
        protocol TEXT,
        path TEXT,
        query TEXT,
        FOREIGN KEY(entry_id) REFERENCES clipboard_entries(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS images (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        entry_id INTEGER NOT NULL,
        file_path TEXT NOT NULL,
        thumbnail_path TEXT,
        width INTEGER,
        height INTEGER,
        file_size INTEGER,
        ocr_text TEXT,
        FOREIGN KEY(entry_id) REFERENCES clipboard_entries(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS files (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        entry_id INTEGER NOT NULL,
        file_path TEXT NOT NULL,
        file_name TEXT,
        extension TEXT,
        exists_on_disk INTEGER,
        FOREIGN KEY(entry_id) REFERENCES clipboard_entries(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS tags (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL UNIQUE
    );

    CREATE TABLE IF NOT EXISTS entry_tags (
        entry_id INTEGER NOT NULL,
        tag_id INTEGER NOT NULL,
        PRIMARY KEY(entry_id, tag_id),
        FOREIGN KEY(entry_id) REFERENCES clipboard_entries(id) ON DELETE CASCADE,
        FOREIGN KEY(tag_id) REFERENCES tags(id) ON DELETE CASCADE
    );

    -- External-content FTS index over clipboard_entries.
    CREATE VIRTUAL TABLE IF NOT EXISTS clipboard_fts USING fts5(
        content,
        source_app,
        window_title,
        tags,
        ocr_text,
        content='clipboard_entries',
        content_rowid='id'
    );

    -- Keep clipboard_fts in sync. tags/ocr_text are not columns on
    -- clipboard_entries yet, so they are indexed as empty here and can be
    -- rebuilt when those features land.
    CREATE TRIGGER IF NOT EXISTS trg_entries_ai AFTER INSERT ON clipboard_entries BEGIN
        INSERT INTO clipboard_fts(rowid, content, source_app, window_title, tags, ocr_text)
        VALUES (new.id, new.content, new.source_app, new.window_title, '', '');
    END;

    CREATE TRIGGER IF NOT EXISTS trg_entries_ad AFTER DELETE ON clipboard_entries BEGIN
        INSERT INTO clipboard_fts(clipboard_fts, rowid, content, source_app, window_title, tags, ocr_text)
        VALUES ('delete', old.id, old.content, old.source_app, old.window_title, '', '');
    END;

    CREATE TRIGGER IF NOT EXISTS trg_entries_au AFTER UPDATE ON clipboard_entries BEGIN
        INSERT INTO clipboard_fts(clipboard_fts, rowid, content, source_app, window_title, tags, ocr_text)
        VALUES ('delete', old.id, old.content, old.source_app, old.window_title, '', '');
        INSERT INTO clipboard_fts(rowid, content, source_app, window_title, tags, ocr_text)
        VALUES (new.id, new.content, new.source_app, new.window_title, '', '');
    END;
    """;
}
