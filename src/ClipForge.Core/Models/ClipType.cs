namespace ClipForge.Core.Models;

/// <summary>
/// Deterministic, rule-based content classification. Persisted as the lowercase
/// name in <c>clipboard_entries.type</c>.
/// </summary>
public enum ClipType
{
    Text,
    Url,
    Email,
    Phone,
    FilePath,
    Color,
    Code,
    LongText,
    Image,
    Files,
    Html
}
