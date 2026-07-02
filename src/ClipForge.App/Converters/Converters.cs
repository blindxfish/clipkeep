using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ClipForge.Core.Models;

namespace ClipForge.App.Converters;

/// <summary>true → Visible, false → Collapsed.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}

/// <summary>null → Visible (shows placeholder), non-null → Collapsed.</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>non-null → Visible, null → Collapsed (mirror of NullToVisibilityConverter).</summary>
public sealed class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Non-empty string → Visible, empty/null → Collapsed.</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Empty/null string → Visible (e.g. show a placeholder icon), non-empty → Collapsed.</summary>
public sealed class EmptyStringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>DateTimeOffset → local short date + time string (e.g. "7/2/2026 2:35 PM").</summary>
public sealed class LocalDateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTimeOffset dt ? dt.ToLocalTime().ToString("g", culture) : null;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// ClipType → a Segoe MDL2 Assets glyph representing that kind of content.
/// Glyphs are given as numeric code points to keep this source ASCII-only.
/// </summary>
public sealed class TypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ClipType t ? Glyph(t) : "";

    public static string Glyph(ClipType t)
    {
        int code = t switch
        {
            ClipType.Url => 0xE71B,       // Link
            ClipType.Email => 0xE715,     // Mail
            ClipType.Phone => 0xE717,     // Phone
            ClipType.FilePath => 0xE8B7,  // Folder
            ClipType.Color => 0xE790,     // Color
            ClipType.Code => 0xE943,      // Code
            ClipType.LongText => 0xE7C3,  // Page
            ClipType.Image => 0xEB9F,     // Photo
            ClipType.Files => 0xE8B7,     // Folder
            ClipType.Html => 0xE774,      // Globe
            _ => 0xE8A5,                  // Document (Text/default)
        };
        return ((char)code).ToString();
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>true → filled star glyph, false → outline star glyph (Segoe MDL2 Assets).</summary>
public sealed class FavoriteToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        ((char)(value is true ? 0xE735 : 0xE734)).ToString();   // FavoriteStarFill : FavoriteStar

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>File path → cached ImageSource (loaded eagerly so the file isn't locked).</summary>
public sealed class PathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
