namespace ClipForge.Core.Settings;

public enum SensitivePolicy
{
    /// <summary>Detected secrets are never persisted (default).</summary>
    DoNotSave,
    /// <summary>Secrets are stored like any other clip.</summary>
    SaveNormally
}

public enum RetentionPeriod
{
    Forever,
    Days30,
    Days90,
    Year1
}

/// <summary>
/// User-configurable settings, persisted as JSON. Defaults are privacy-first:
/// monitoring on for text, password managers excluded, secrets not saved.
/// </summary>
public sealed class AppSettings
{
    // General
    public bool LaunchOnStartup { get; set; }
    public bool StartMinimized { get; set; }
    public bool ShowNotifications { get; set; } = true;
    public bool ConfirmBeforeDelete { get; set; } = true;

    // Clipboard monitoring
    public bool EnableMonitoring { get; set; } = true;
    public bool MonitorText { get; set; } = true;
    public bool MonitorImages { get; set; } = true;
    public bool MonitorFiles { get; set; } = true;
    public bool MonitorHtml { get; set; } = true;

    // Privacy
    public SensitivePolicy SensitivePolicy { get; set; } = SensitivePolicy.DoNotSave;
    public List<string> ExcludedApps { get; set; } = DefaultExcludedApps();

    // Storage
    public int? MaxDatabaseSizeMb { get; set; }
    public RetentionPeriod Retention { get; set; } = RetentionPeriod.Forever;

    public static List<string> DefaultExcludedApps() => new()
    {
        "Bitwarden",
        "1Password",
        "KeePass",
        "KeePassXC",
        "NordPass",
        "Dashlane"
    };

    /// <summary>
    /// True if a clip from the given process/app should be ignored. Matching is
    /// case-insensitive and ignores a trailing ".exe" on either side.
    /// </summary>
    public bool IsAppExcluded(string? processName, string? appName)
    {
        foreach (var raw in ExcludedApps)
        {
            var excluded = Normalize(raw);
            if (excluded.Length == 0) continue;
            if (excluded == Normalize(processName) || excluded == Normalize(appName))
                return true;
        }
        return false;
    }

    private static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var trimmed = name.Trim();
        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^4];
        return trimmed.ToLowerInvariant();
    }
}
