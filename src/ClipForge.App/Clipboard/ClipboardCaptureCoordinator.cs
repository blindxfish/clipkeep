using System.Windows.Media.Imaging;
using ClipForge.App.Storage;
using ClipForge.Core.Models;
using ClipForge.Core.Services;
using ClipForge.Core.Settings;
using ClipForge.Core.Storage;
using ClipForge.Infrastructure.WindowsApi;
using WpfClipboard = System.Windows.Clipboard;

namespace ClipForge.App.Clipboard;

/// <summary>
/// Bridges the raw <see cref="ClipboardListener"/> to the storage pipeline: reads
/// the clipboard (image first, then text), captures the source app, stores the
/// entry, and signals the UI. Ignores self-originated writes to avoid capture loops.
/// </summary>
public sealed class ClipboardCaptureCoordinator : IDisposable
{
    private readonly ClipboardListener _listener;
    private readonly ClipboardStorageService _storage;
    private readonly ForegroundWindowTracker _tracker;
    private readonly ISettingsService _settings;
    private readonly IClipRepository _repository;
    private readonly ImageStore _imageStore;

    private bool _paused;
    private bool _suppressNext;

    public ClipboardCaptureCoordinator(
        ClipboardListener listener,
        ClipboardStorageService storage,
        ForegroundWindowTracker tracker,
        ISettingsService settings,
        IClipRepository repository,
        ImageStore imageStore)
    {
        _listener = listener;
        _storage = storage;
        _tracker = tracker;
        _settings = settings;
        _repository = repository;
        _imageStore = imageStore;
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
        if (!settings.EnableMonitoring) return;

        var source = _tracker.Capture();

        // Respect the application blacklist (e.g. password managers).
        if (settings.IsAppExcluded(source.ProcessName, source.AppName)) return;

        // Prefer an image when present (e.g. screenshots), otherwise fall back to text.
        if (settings.MonitorImages && TryReadImage() is { } bitmap)
        {
            var result = StoreImage(bitmap, source);
            if (result is not null) EntryStored?.Invoke(this, result);
            return;
        }

        if (!settings.MonitorText) return;

        var text = TryReadText();
        if (string.IsNullOrEmpty(text)) return;

        var capture = new ClipboardCapture
        {
            Text = text,
            SourceApp = source.AppName,
            SourceProcess = source.ProcessName,
            WindowTitle = source.WindowTitle
        };

        var textResult = _storage.Store(capture);
        if (textResult is not null)
            EntryStored?.Invoke(this, textResult);
    }

    /// <summary>Dedup by pixel hash; save to disk and insert only when new.</summary>
    private StoreResult? StoreImage(BitmapSource bitmap, SourceInfo source)
    {
        var now = DateTimeOffset.UtcNow;
        var hash = ImageStore.HashPixels(bitmap);

        var existing = _repository.GetByHash(hash);
        if (existing is not null)
        {
            _repository.TouchDuplicate(existing.Id, now);
            existing.CopyCount += 1;
            existing.LastCopiedAt = now;
            return new StoreResult(existing, false, new ClipClassification { Type = ClipType.Image });
        }

        var saved = _imageStore.Save(bitmap, now);
        var entry = new ClipEntry
        {
            Type = ClipType.Image,
            Content = $"Image {saved.Record.Width}×{saved.Record.Height}",
            ContentHash = hash,
            SourceApp = source.AppName,
            SourceProcess = source.ProcessName,
            WindowTitle = source.WindowTitle,
            CopyCount = 1,
            FirstCopiedAt = now,
            LastCopiedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _repository.InsertImage(entry, saved.Record);
        return new StoreResult(entry, true, new ClipClassification { Type = ClipType.Image });
    }

    /// <summary>Read a clipboard bitmap with retries, or null if none/locked.</summary>
    private static BitmapSource? TryReadImage()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                return WpfClipboard.ContainsImage() ? WpfClipboard.GetImage() : null;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                Thread.Sleep(15);
            }
        }
        return null;
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
