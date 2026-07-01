namespace ClipForge.Core.Models;

/// <summary>
/// A stored clipboard entry. Mirrors the <c>clipboard_entries</c> table.
/// </summary>
public sealed class ClipEntry
{
    public long Id { get; set; }
    public ClipType Type { get; set; }
    public string? Content { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string? SourceApp { get; set; }
    public string? SourceProcess { get; set; }
    public string? WindowTitle { get; set; }
    public bool Favorite { get; set; }
    public int CopyCount { get; set; } = 1;
    public DateTimeOffset FirstCopiedAt { get; set; }
    public DateTimeOffset LastCopiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
