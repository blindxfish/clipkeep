using ClipForge.Core.Models;
using ClipForge.Core.Storage;
using ClipForge.Infrastructure.Database;
using ClipForge.Infrastructure.Repositories;
using ClipForge.Infrastructure.Storage;

namespace ClipForge.Tests;

/// <summary>
/// Exercises the image persistence path over a real temp DB. Files aren't written
/// here (the repository only stores paths/metadata), so paths are synthetic.
/// </summary>
public sealed class ImageRepositoryTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly ClipRepository _repo;

    public ImageRepositoryTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "clipforge_img_" + Guid.NewGuid().ToString("N"));
        var db = new ClipDatabase(new AppPaths(_tempRoot));
        db.Initialize();
        _repo = new ClipRepository(db);
    }

    private ClipEntry NewImageEntry(string hash)
    {
        var now = DateTimeOffset.UtcNow;
        return new ClipEntry
        {
            Type = ClipType.Image,
            Content = "Image 800×600",
            ContentHash = hash,
            SourceApp = "SnippingTool.exe",
            CopyCount = 1,
            FirstCopiedAt = now,
            LastCopiedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    [Fact]
    public void InsertImage_persists_entry_and_image_row()
    {
        var entry = _repo.InsertImage(NewImageEntry("hash-a"), new ImageRecord
        {
            FilePath = @"C:\img\a.png",
            ThumbnailPath = @"C:\thumb\a.jpg",
            Width = 800,
            Height = 600,
            FileSize = 12345,
        });

        Assert.True(entry.Id > 0);

        var image = _repo.GetImage(entry.Id);
        Assert.NotNull(image);
        Assert.Equal(@"C:\img\a.png", image!.FilePath);
        Assert.Equal(800, image.Width);
        Assert.Equal(600, image.Height);
    }

    [Fact]
    public void Query_populates_thumbnail_path_for_image_entries()
    {
        var entry = _repo.InsertImage(NewImageEntry("hash-b"), new ImageRecord
        {
            FilePath = @"C:\img\b.png",
            ThumbnailPath = @"C:\thumb\b.jpg",
            Width = 100, Height = 100, FileSize = 1,
        });

        var listed = _repo.Query(new ClipQuery()).Single(e => e.Id == entry.Id);
        Assert.Equal(@"C:\thumb\b.jpg", listed.ThumbnailPath);
    }

    [Fact]
    public void GetByHash_and_TouchDuplicate_dedup_images()
    {
        var entry = _repo.InsertImage(NewImageEntry("hash-c"), new ImageRecord
        {
            FilePath = @"C:\img\c.png", Width = 10, Height = 10, FileSize = 1,
        });

        var found = _repo.GetByHash("hash-c");
        Assert.NotNull(found);

        _repo.TouchDuplicate(found!.Id, DateTimeOffset.UtcNow);
        Assert.Equal(2, _repo.GetById(entry.Id)!.CopyCount);
    }

    [Fact]
    public void Deleting_image_entry_cascades_to_images_row()
    {
        var entry = _repo.InsertImage(NewImageEntry("hash-d"), new ImageRecord
        {
            FilePath = @"C:\img\d.png", Width = 10, Height = 10, FileSize = 1,
        });

        _repo.Delete(entry.Id);

        Assert.Null(_repo.GetImage(entry.Id));
        Assert.Null(_repo.GetById(entry.Id));
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempRoot, recursive: true); } catch { }
    }
}
