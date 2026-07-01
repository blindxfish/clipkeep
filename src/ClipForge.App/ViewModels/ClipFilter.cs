using ClipForge.Core.Models;

namespace ClipForge.App.ViewModels;

/// <summary>A sidebar category: a label plus the query constraints it applies.</summary>
public sealed record ClipFilter(string Label, ClipType? Type, bool FavoritesOnly = false)
{
    public static readonly IReadOnlyList<ClipFilter> Sidebar = new[]
    {
        new ClipFilter("All Clips", null),
        new ClipFilter("Text", ClipType.Text),
        new ClipFilter("URLs", ClipType.Url),
        new ClipFilter("Code", ClipType.Code),
        new ClipFilter("Files", ClipType.FilePath),
        new ClipFilter("Images", ClipType.Image),
        new ClipFilter("Favorites", null, FavoritesOnly: true),
    };
}
