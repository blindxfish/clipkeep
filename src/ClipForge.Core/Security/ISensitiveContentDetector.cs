namespace ClipForge.Core.Security;

public interface ISensitiveContentDetector
{
    /// <summary>True if the text looks like a secret that should not be persisted.</summary>
    bool IsSensitive(string text);
}
