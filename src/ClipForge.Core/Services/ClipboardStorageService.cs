using ClipForge.Core.Classification;
using ClipForge.Core.Models;
using ClipForge.Core.Security;
using ClipForge.Core.Storage;

namespace ClipForge.Core.Services;

/// <summary>
/// The capture pipeline: classify → screen for secrets → hash → dedup-upsert.
/// Sensitive content is dropped BEFORE persistence so secrets never hit disk
/// (this is the default policy per the spec; a future setting can relax it).
/// </summary>
public sealed class ClipboardStorageService
{
    private readonly IClipRepository _repository;
    private readonly IClassificationService _classifier;
    private readonly ISensitiveContentDetector _sensitive;
    private readonly Func<DateTimeOffset> _now;

    public ClipboardStorageService(
        IClipRepository repository,
        IClassificationService classifier,
        ISensitiveContentDetector sensitive,
        Func<DateTimeOffset>? clock = null)
    {
        _repository = repository;
        _classifier = classifier;
        _sensitive = sensitive;
        _now = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Process one captured clip. Returns the stored entry, or null when the clip
    /// was ignored (empty or sensitive).
    /// </summary>
    public StoreResult? Store(ClipboardCapture capture)
    {
        if (string.IsNullOrWhiteSpace(capture.Text))
            return null;

        if (_sensitive.IsSensitive(capture.Text))
            return null;

        var classification = _classifier.Classify(capture.Text);
        var now = _now();

        var entry = new ClipEntry
        {
            Type = classification.Type,
            Content = capture.Text,
            ContentHash = ContentHasher.HashText(capture.Text),
            SourceApp = capture.SourceApp,
            SourceProcess = capture.SourceProcess,
            WindowTitle = capture.WindowTitle,
            CopyCount = 1,
            FirstCopiedAt = now,
            LastCopiedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var (stored, isNew) = _repository.Upsert(entry);
        return new StoreResult(stored, isNew, classification);
    }
}

public sealed record StoreResult(ClipEntry Entry, bool IsNew, ClipClassification Classification);
