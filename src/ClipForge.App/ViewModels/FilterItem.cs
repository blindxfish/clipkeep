using CommunityToolkit.Mvvm.ComponentModel;
using ClipForge.Core.Storage;

namespace ClipForge.App.ViewModels;

/// <summary>
/// A sidebar row: its <see cref="ClipFilter"/> plus a live entry count for the
/// badge. Wraps <see cref="ClipFilter"/> so the count can change independently
/// of the (immutable) filter definition.
/// </summary>
public sealed partial class FilterItem : ObservableObject
{
    public FilterItem(ClipFilter filter) => Filter = filter;

    public ClipFilter Filter { get; }

    public string Label => Filter.Label;
    public string Icon => Filter.Icon;

    /// <summary>Number of entries this filter currently matches (badge value).</summary>
    [ObservableProperty]
    private int _count;

    /// <summary>Recompute this row's badge from the latest tallies.</summary>
    public void UpdateCount(ClipCounts counts) => Count = Filter switch
    {
        { FavoritesOnly: true } => counts.Favorites,
        { Type: { } type } => counts.For(type),
        _ => counts.Total,
    };
}
