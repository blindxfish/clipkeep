using ClipForge.Core.Models;

namespace ClipForge.Core.Storage;

/// <summary>Ordering for a listing query (ignored while a text search is active,
/// where results are ranked by relevance).</summary>
public enum ClipSort
{
    NewestFirst,
    OldestFirst,
}

/// <summary>Filter/paging options for listing and searching entries.</summary>
public sealed class ClipQuery
{
    /// <summary>Free-text search; when null/empty, returns recent entries.</summary>
    public string? Search { get; init; }

    /// <summary>Restrict to a single type (e.g. sidebar "URLs" view).</summary>
    public ClipType? Type { get; init; }

    /// <summary>Only favorites.</summary>
    public bool FavoritesOnly { get; init; }

    /// <summary>Inclusive lower bound on <c>last_copied_at</c>; null means unbounded.</summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>Inclusive upper bound on <c>last_copied_at</c>; null means unbounded.</summary>
    public DateTimeOffset? To { get; init; }

    /// <summary>Ordering for non-search listings; search results are ranked by relevance.</summary>
    public ClipSort Sort { get; init; } = ClipSort.NewestFirst;

    public int Limit { get; init; } = 200;
    public int Offset { get; init; }
}
