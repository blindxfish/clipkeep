using ClipForge.Core.Models;
using ClipForge.Core.Services;
using ClipForge.Core.Settings;
using ClipForge.Infrastructure.WindowsApi;
using WpfClipboard = System.Windows.Clipboard;

namespace ClipForge.App.Clipboard;

/// <summary>
/// Bridges the raw <see cref="ClipboardListener"/> to the storage pipeline: reads
/// the clipboard, captures the source app, stores the entry, and signals the UI.
/// Ignores self-originated writes to avoid capture loops.
/// </summary>
public sealed class ClipboardCaptureCoordinator : IDisposable
{
    private readonly ClipboardListener _listener;
    private readonly ClipboardStorageService _storage;
    private readonly ForegroundWindowTracker _tracker;
    private readonly ISettingsService _settings;

    private bool _paused;
    private bool _suppressNext;

    public ClipboardCaptureCoordinator(
        ClipboardListener listener,
        ClipboardStorageService storage,
        ForegroundWindowTracker tracker,
        ISettingsService settings)
    {
        _listener = listener;
        _storage = storage;
        _tracker = tracker;
        _settings = settings;
    }

    /// <summary>Raised on the UI thread after a clip is stored (new or deduped).</summary>
    public event EventHandler<StoreResult>? EntryStored;

    public bool IsPaused
    {
        get => _paused;
        set => _paused = value;
    }

    public void Start()
    {
        _listener.ClipboardUpdated += OnClipboardUpdated;
        _listener.Start();
    }

    /// <summary>
    /// Call immediately before ClipForge itself writes to the clipboard so the
    /// resulting WM_CLIPBOARDUPDATE is not re-captured.
    /// </summary>
    public void SuppressNextChange() => _suppressNext = true;

    private void OnClipboardUpdated(object? sender, EventArgs e)
    {
        if (_suppressNext)
        {
            _suppressNext = false;
            return;
        }
        if (_paused) return;

        var settings = _settings.Current;
        if (!settings.EnableMonitoring || !settings.MonitorText) return;

        var text = TryReadText();
        if (string.IsNullOrEmpty(text)) return;

        var source = _tracker.Capture();

        // Respect the application blacklist (e.g. password managers).
        if (settings.IsAppExcluded(source.ProcessName, source.AppName)) return;

        var capture = new ClipboardCapture
        {
            Text = text,
            SourceApp = source.AppName,
            SourceProcess = source.ProcessName,
            WindowTitle = source.WindowTitle
        };

        var result = _storage.Store(capture);
        if (result is not null)
            EntryStored?.Invoke(this, result);
    }

    /// <summary>
    /// Read clipboard text with a few short retries — the clipboard is frequently
    /// locked by the app that just wrote to it.
    /// </summary>
    private static string? TryReadText()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                return WpfClipboard.ContainsText() ? WpfClipboard.GetText() : null;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // Clipboard is locked by the writer; back off briefly and retry.
                Thread.Sleep(15);
            }
        }
        return null;
    }

    public void Dispose()
    {
        _listener.ClipboardUpdated -= OnClipboardUpdated;
    }
}
