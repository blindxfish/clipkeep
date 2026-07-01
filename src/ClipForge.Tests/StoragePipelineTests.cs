using ClipForge.Core.Classification;
using ClipForge.Core.Models;
using ClipForge.Core.Security;
using ClipForge.Core.Services;
using ClipForge.Core.Storage;
using ClipForge.Infrastructure.Database;
using ClipForge.Infrastructure.Repositories;
using ClipForge.Infrastructure.Storage;

namespace ClipForge.Tests;

/// <summary>
/// End-to-end tests over a real temp SQLite DB: capture → classify → dedup → FTS.
/// </summary>
public sealed class StoragePipelineTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly ClipRepository _repo;
    private readonly ClipboardStorageService _pipeline;

    public StoragePipelineTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "clipforge_test_" + Guid.NewGuid().ToString("N"));
        var paths = new AppPaths(_tempRoot);
        var db = new ClipDatabase(paths);
        db.Initialize();
        _repo = new ClipRepository(db);
        var settings = new JsonSettingsService(paths);
        _pipeline = new ClipboardStorageService(_repo, new ClassificationService(), new SensitiveContentDetector(), settings);
    }

    [Fact]
    public void Stores_new_text_entry()
    {
        var result = _pipeline.Store(new ClipboardCapture { Text = "hello world", SourceApp = "notepad.exe" });

        Assert.NotNull(result);
        Assert.True(result!.IsNew);
        Assert.True(result.Entry.Id > 0);
        Assert.Equal(ClipType.Text, result.Entry.Type);
    }

    [Fact]
    public void Duplicate_bumps_copy_count_instead_of_inserting()
    {
        _pipeline.Store(new ClipboardCapture { Text = "repeated" });
        var second = _pipeline.Store(new ClipboardCapture { Text = "repeated" });

        Assert.NotNull(second);
        Assert.False(second!.IsNew);
        Assert.Equal(2, second.Entry.CopyCount);

        var all = _repo.Query(new ClipQuery());
        Assert.Single(all);
    }

    [Fact]
    public void Sensitive_content_is_not_persisted()
    {
        var result = _pipeline.Store(new ClipboardCapture { Text = "sk-abcdefghijklmnopqrstuvwxyz0123" });

        Assert.Null(result);
        Assert.Empty(_repo.Query(new ClipQuery()));
    }

    [Fact]
    public void Fts_search_finds_by_content()
    {
        _pipeline.Store(new ClipboardCapture { Text = "docker compose up -d" });
        _pipeline.Store(new ClipboardCapture { Text = "unrelated note" });

        var hits = _repo.Query(new ClipQuery { Search = "docker" });

        Assert.Single(hits);
        Assert.Contains("docker", hits[0].Content);
    }

    [Fact]
    public void Fts_search_matches_prefix()
    {
        _pipeline.Store(new ClipboardCapture { Text = "PORD004758 reference" });

        var hits = _repo.Query(new ClipQuery { Search = "PORD" });

        Assert.Single(hits);
    }

    [Fact]
    public void Delete_removes_from_search_index()
    {
        var stored = _pipeline.Store(new ClipboardCapture { Text = "ephemeral entry" })!;
        _repo.Delete(stored.Entry.Id);

        Assert.Empty(_repo.Query(new ClipQuery { Search = "ephemeral" }));
    }

    [Fact]
    public void PurgeOlderThan_removes_aged_non_favorites_but_keeps_favorites()
    {
        var oldFav = _pipeline.Store(new ClipboardCapture { Text = "old favorite" })!;
        var oldPlain = _pipeline.Store(new ClipboardCapture { Text = "old plain" })!;
        _repo.SetFavorite(oldFav.Entry.Id, true);

        // Purge everything older than "now + 1 day" so both entries qualify by age.
        var removed = _repo.PurgeOlderThan(DateTimeOffset.UtcNow.AddDays(1));

        Assert.Equal(1, removed);
        Assert.NotNull(_repo.GetById(oldFav.Entry.Id));   // favorite survives
        Assert.Null(_repo.GetById(oldPlain.Entry.Id));    // non-favorite purged
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempRoot, recursive: true); }
        catch { /* best-effort cleanup */ }
    }
}
