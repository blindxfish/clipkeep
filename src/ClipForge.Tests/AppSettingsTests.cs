using ClipForge.Core.Settings;
using ClipForge.Infrastructure.Storage;

namespace ClipForge.Tests;

public class AppSettingsTests
{
    [Theory]
    [InlineData("Bitwarden", null)]
    [InlineData("bitwarden", null)]
    [InlineData(null, "Bitwarden.exe")]
    [InlineData("KeePassXC", null)]
    public void Excluded_apps_are_matched(string? process, string? app)
    {
        var settings = new AppSettings();
        Assert.True(settings.IsAppExcluded(process, app));
    }

    [Theory]
    [InlineData("notepad", "notepad.exe")]
    [InlineData("chrome", "chrome.exe")]
    [InlineData(null, null)]
    public void Ordinary_apps_are_not_excluded(string? process, string? app)
    {
        var settings = new AppSettings();
        Assert.False(settings.IsAppExcluded(process, app));
    }
}

public sealed class JsonSettingsServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly AppPaths _paths;

    public JsonSettingsServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "clipforge_settings_" + Guid.NewGuid().ToString("N"));
        _paths = new AppPaths(_tempRoot);
    }

    [Fact]
    public void Defaults_are_used_when_no_file_exists()
    {
        var svc = new JsonSettingsService(_paths);
        Assert.True(svc.Current.EnableMonitoring);
        Assert.Equal(SensitivePolicy.DoNotSave, svc.Current.SensitivePolicy);
        Assert.Contains("Bitwarden", svc.Current.ExcludedApps);
    }

    [Fact]
    public void Saved_settings_round_trip_across_instances()
    {
        var first = new JsonSettingsService(_paths);
        var updated = first.Current;
        updated.EnableMonitoring = false;
        updated.SensitivePolicy = SensitivePolicy.SaveNormally;
        updated.ExcludedApps.Add("MyVault");
        first.Save(updated);

        var reloaded = new JsonSettingsService(_paths);
        Assert.False(reloaded.Current.EnableMonitoring);
        Assert.Equal(SensitivePolicy.SaveNormally, reloaded.Current.SensitivePolicy);
        Assert.Contains("MyVault", reloaded.Current.ExcludedApps);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { }
    }
}
