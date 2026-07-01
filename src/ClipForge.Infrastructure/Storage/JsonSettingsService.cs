using System.Text.Json;
using System.Text.Json.Serialization;
using ClipForge.Core.Settings;

namespace ClipForge.Infrastructure.Storage;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON at %AppData%\ClipForge\settings.json.
/// Missing/corrupt files fall back to defaults so the app always starts.
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public AppSettings Current { get; private set; }

    public event EventHandler? Changed;

    public JsonSettingsService(AppPaths paths)
    {
        paths.EnsureCreated();
        _filePath = Path.Combine(paths.DataDir, "settings.json");
        Current = Load();
    }

    public void Save(AppSettings settings)
    {
        Current = settings;
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, Options));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, Options);
                if (loaded is not null) return loaded;
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Corrupt or unreadable settings — fall through to defaults.
        }

        var defaults = new AppSettings();
        try { File.WriteAllText(_filePath, JsonSerializer.Serialize(defaults, Options)); }
        catch (IOException) { /* best effort */ }
        return defaults;
    }
}
