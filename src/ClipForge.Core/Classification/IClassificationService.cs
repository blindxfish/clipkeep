using ClipForge.Core.Models;

namespace ClipForge.Core.Classification;

public interface IClassificationService
{
    /// <summary>
    /// Deterministically classify a piece of clipboard text. Never returns null;
    /// falls back to <see cref="ClipType.Text"/>.
    /// </summary>
    ClipClassification Classify(string text);
}
