using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClipForge.Core.Models;
using ClipForge.Infrastructure.Storage;

namespace ClipForge.App.Storage;

/// <summary>Result of persisting a clipboard image: its stable hash + metadata.</summary>
public sealed record ImageSaveResult(string Hash, ImageRecord Record);

/// <summary>
/// Persists clipboard bitmaps to disk as PNG plus a JPEG thumbnail, organized by
/// year/month, and computes a stable content hash from the raw pixels (so the
/// hash is independent of encoding). The DB only ever stores the resulting paths.
/// </summary>
public sealed class ImageStore
{
    private const int ThumbnailMaxEdge = 200;
    private readonly AppPaths _paths;

    public ImageStore(AppPaths paths) => _paths = paths;

    /// <summary>Compute the pixel hash without writing anything (for dedup checks).</summary>
    public static string HashPixels(BitmapSource source)
    {
        var normalized = ToBgra32(source);
        var stride = normalized.PixelWidth * 4;
        var buffer = new byte[stride * normalized.PixelHeight];
        normalized.CopyPixels(buffer, stride, 0);
        return Convert.ToHexString(SHA256.HashData(buffer)).ToLowerInvariant();
    }

    /// <summary>Encode + save the image and its thumbnail; returns hash + metadata.</summary>
    public ImageSaveResult Save(BitmapSource source, DateTimeOffset now)
    {
        var hash = HashPixels(source);
        var subDir = Path.Combine(now.Year.ToString("D4"), now.Month.ToString("D2"));

        var imageDir = Path.Combine(_paths.ImagesDir, subDir);
        var thumbDir = Path.Combine(_paths.ThumbnailsDir, subDir);
        Directory.CreateDirectory(imageDir);
        Directory.CreateDirectory(thumbDir);

        var imagePath = Path.Combine(imageDir, hash + ".png");
        var thumbPath = Path.Combine(thumbDir, hash + "_thumb.jpg");

        SavePng(source, imagePath);
        SaveThumbnail(source, thumbPath);

        var record = new ImageRecord
        {
            FilePath = imagePath,
            ThumbnailPath = thumbPath,
            Width = source.PixelWidth,
            Height = source.PixelHeight,
            FileSize = new FileInfo(imagePath).Length,
        };
        return new ImageSaveResult(hash, record);
    }

    /// <summary>Delete an image's on-disk files (original + thumbnail). Best-effort.</summary>
    public void DeleteFiles(ImageRecord image)
    {
        TryDelete(image.FilePath);
        if (image.ThumbnailPath is not null) TryDelete(image.ThumbnailPath);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { /* file locked/removed; ignore */ }
        catch (UnauthorizedAccessException) { /* ignore */ }
    }

    private static void SavePng(BitmapSource source, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void SaveThumbnail(BitmapSource source, string path)
    {
        var scale = Math.Min(1.0,
            (double)ThumbnailMaxEdge / Math.Max(source.PixelWidth, source.PixelHeight));
        BitmapSource thumb = scale < 1.0
            ? new TransformedBitmap(source, new ScaleTransform(scale, scale))
            : source;

        var encoder = new JpegBitmapEncoder { QualityLevel = 80 };
        encoder.Frames.Add(BitmapFrame.Create(thumb));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static BitmapSource ToBgra32(BitmapSource source) =>
        source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
}
