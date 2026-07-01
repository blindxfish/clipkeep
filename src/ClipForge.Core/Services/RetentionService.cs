using ClipForge.Core.Settings;
using ClipForge.Core.Storage;

namespace ClipForge.Core.Services;

/// <summary>
/// Applies the configured retention period by purging aged, non-favorite entries.
/// Safe to call repeatedly (on startup and on a timer).
/// </summary>
public sealed class RetentionService
{
    private readonly IClipRepository _repository;
    private readonly ISettingsService _settings;
    private readonly Func<DateTimeOffset> _now;

    public RetentionService(
        IClipRepository repository,
        ISettingsService settings,
        Func<DateTimeOffset>? clock = null)
    {
        _repository = repository;
        _settings = settings;
        _now = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>Run one cleanup pass; returns how many entries were removed.</summary>
    public int RunCleanup()
    {
        var maxAge = RetentionPolicy.MaxAge(_settings.Current.Retention);
        if (maxAge is not { } age) return 0; // Forever

        var cutoff = _now() - age;
        return _repository.PurgeOlderThan(cutoff);
    }
}
