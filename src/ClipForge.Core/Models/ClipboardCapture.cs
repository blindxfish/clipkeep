namespace ClipForge.Core.Models;

/// <summary>
/// Raw material captured from a clipboard event, before classification and
/// storage. Currently text-only (images/files arrive in a later slice).
/// </summary>
public sealed class ClipboardCapture
{
    public required string Text { get; init; }
    public string? SourceApp { get; init; }
    public string? SourceProcess { get; init; }
    public string? WindowTitle { get; init; }
}
