using ClipForge.Core.Models;

namespace ClipForge.Core.Storage;

/// <summary>
/// Entry tallies used to populate the sidebar badges: the grand total, the
/// number of favorites, and a per-type breakdown.
/// </summary>
public sealed record ClipCounts(
    int Total,
    int Favorites,
    IReadOnlyDictionary<ClipType, int> ByType)
{
    public static readonly ClipCounts Empty =
        new(0, 0, new Dictionary<ClipType, int>());

    /// <summary>Count for a single type, or 0 if none are stored.</summary>
    public int For(ClipType type) => ByType.TryGetValue(type, out var n) ? n : 0;
}
