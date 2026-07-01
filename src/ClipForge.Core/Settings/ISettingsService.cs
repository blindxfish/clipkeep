namespace ClipForge.Core.Settings;

/// <summary>
/// Loads and persists <see cref="AppSettings"/>. <see cref="Current"/> is the live
/// snapshot read by the capture pipeline; <see cref="Save"/> replaces and persists it.
/// </summary>
public interface ISettingsService
{
    AppSettings Current { get; }

    /// <summary>Raised after settings change so listeners can react (e.g. hotkeys).</summary>
    event EventHandler? Changed;

    void Save(AppSettings settings);
}
