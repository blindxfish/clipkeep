using ClipForge.Core.Models;

namespace ClipForge.App.ViewModels;

/// <summary>A sidebar category: a label, an icon glyph, and the query constraints it applies.</summary>
public sealed record ClipFilter(string Label, ClipType? Type, int IconCode, bool FavoritesOnly = false)
{
    /// <summary>The icon as a Segoe MDL2 Assets glyph string.</summary>
    public string Icon => ((char)IconCode).ToString();

    public static readonly IReadOnlyList<ClipFilter> Sidebar = new[]
    {
        new ClipFilter("All Clips", null, 0xE8A5),                       // Document
        new ClipFilter("Text", ClipType.Text, 0xE8FD),                  // BulletedList
        new ClipFilter("URLs", ClipType.Url, 0xE71B),                   // Link
        new ClipFilter("Code", ClipType.Code, 0xE943),                  // Code
        new ClipFilter("Files", ClipType.FilePath, 0xE8B7),             // Folder
        new ClipFilter("Images", ClipType.Image, 0xEB9F),              // Photo
        new ClipFilter("Favorites", null, 0xE734, FavoritesOnly: true), // Star
    };
}
