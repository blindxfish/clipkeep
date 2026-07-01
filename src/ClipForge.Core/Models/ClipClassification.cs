namespace ClipForge.Core.Models;

/// <summary>
/// Result of classifying a piece of text: its primary type plus any extracted
/// tags/metadata (e.g. detected code language, url domain).
/// </summary>
public sealed class ClipClassification
{
    public required ClipType Type { get; init; }

    /// <summary>Detected programming language when <see cref="Type"/> is Code.</summary>
    public string? CodeLanguage { get; init; }

    /// <summary>Free-form tags used to enrich search (e.g. domain, language).</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
