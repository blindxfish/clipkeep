using System.Security.Cryptography;
using System.Text;

namespace ClipForge.Core.Storage;

/// <summary>
/// Produces the stable content hash used for duplicate detection. Text is
/// normalized (trim + collapse CRLF) so trivially different copies of the same
/// content collapse to one entry.
/// </summary>
public static class ContentHasher
{
    public static string HashText(string text)
    {
        var normalized = Normalize(text);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n").Trim();
}
