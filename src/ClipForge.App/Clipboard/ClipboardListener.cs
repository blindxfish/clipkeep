using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ClipForge.App.Clipboard;

/// <summary>
/// Owns a hidden message-only window that registers for WM_CLIPBOARDUPDATE via
/// AddClipboardFormatListener and raises <see cref="ClipboardUpdated"/> on the UI
/// thread whenever the system clipboard changes.
/// </summary>
public sealed partial class ClipboardListener : IDisposable
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private HwndSource? _source;
    private bool _registered;

    /// <summary>Raised (on the UI thread) after the clipboard content changes.</summary>
    public event EventHandler? ClipboardUpdated;

    public void Start()
    {
        if (_source is not null) return;

        var parameters = new HwndSourceParameters("ClipForgeClipboardListener")
        {
            ParentWindow = HWND_MESSAGE, // message-only window (no UI, receives messages)
            Width = 0,
            Height = 0
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);

        _registered = AddClipboardFormatListener(_source.Handle);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            ClipboardUpdated?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source is not null)
        {
            if (_registered)
            {
                RemoveClipboardFormatListener(_source.Handle);
                _registered = false;
            }
            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AddClipboardFormatListener(IntPtr hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RemoveClipboardFormatListener(IntPtr hwnd);
}
