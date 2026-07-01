namespace ClipForge.Core.Models;

/// <summary>
/// Metadata for a stored image clip. Mirrors the <c>images</c> table. The bitmap
/// itself lives on disk; only paths + dimensions are kept in SQLite.
/// </summary>
public sealed class ImageRecord
{
    public long Id { get; set; }
    public long EntryId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }
    public string? OcrText { get; set; }
}
