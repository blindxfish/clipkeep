using ClipForge.Core.Models;

namespace ClipForge.Core.Storage;

/// <summary>Filter/paging options for listing and searching entries.</summary>
public sealed class ClipQuery
{
    /// <summary>Free-text search; when null/empty, returns recent entries.</summary>
    public string? Search { get; init; }

    /// <summary>Restrict to a single type (e.g. sidebar "URLs" view).</summary>
    public ClipType? Type { get; init; }

    /// <summary>Only favorites.</summary>
    public bool FavoritesOnly { get; init; }

    public int Limit { get; init; } = 200;
    public int Offset { get; init; }
}
