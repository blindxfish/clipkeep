namespace ClipForge.Infrastructure.Storage;

/// <summary>
/// Resolves the on-disk layout under %AppData%\ClipForge (overridable for tests).
/// </summary>
public sealed class AppPaths
{
    public string Root { get; }
    public string DataDir { get; }
    public string ImagesDir { get; }
    public string ThumbnailsDir { get; }
    public string DatabasePath { get; }

    public AppPaths(string? root = null)
    {
        Root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipForge");
        DataDir = Path.Combine(Root, "Data");
        ImagesDir = Path.Combine(Root, "Images");
        ThumbnailsDir = Path.Combine(Root, "Thumbnails");
        DatabasePath = Path.Combine(DataDir, "clipforge.db");
    }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(ImagesDir);
        Directory.CreateDirectory(ThumbnailsDir);
    }
}
