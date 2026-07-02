using ClipForge.Core.Models;

namespace ClipForge.Core.Storage;

/// <summary>
/// Persistence for clipboard entries. Implementations own dedup-on-write:
/// storing an entry whose hash already exists bumps copy_count/last_copied_at
/// instead of inserting a duplicate.
/// </summary>
public interface IClipRepository
{
    /// <summary>
    /// Insert a new entry, or update the existing one if <paramref name="entry"/>'s
    /// content hash already exists. Returns the stored entry (with Id populated)
    /// and whether it was newly inserted.
    /// </summary>
    (ClipEntry Entry, bool IsNew) Upsert(ClipEntry entry);

    IReadOnlyList<ClipEntry> Query(ClipQuery query);

    /// <summary>Entry tallies for the sidebar badges (total, favorites, per type).</summary>
    ClipCounts GetCounts();

    ClipEntry? GetById(long id);

    /// <summary>Look up an entry by its content hash, or null if none exists.</summary>
    ClipEntry? GetByHash(string contentHash);

    /// <summary>Bump copy_count and last_copied/updated on an existing entry.</summary>
    void TouchDuplicate(long id, DateTimeOffset when);

    /// <summary>
    /// Insert an image entry plus its <c>images</c> row atomically. The bitmap must
    /// already be on disk; only paths/metadata are persisted here.
    /// </summary>
    ClipEntry InsertImage(ClipEntry entry, ImageRecord image);

    /// <summary>Image metadata for an entry, or null if it isn't an image.</summary>
    ImageRecord? GetImage(long entryId);

    void SetFavorite(long id, bool favorite);

    void Delete(long id);

    /// <summary>
    /// Delete non-favorite entries last copied before <paramref name="cutoff"/>.
    /// Favorites are never removed by retention. Returns the number deleted.
    /// </summary>
    int PurgeOlderThan(DateTimeOffset cutoff);
}
