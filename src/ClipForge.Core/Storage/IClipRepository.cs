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

    ClipEntry? GetById(long id);

    void SetFavorite(long id, bool favorite);

    void Delete(long id);
}
