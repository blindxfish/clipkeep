using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ClipForge.App.Hotkeys;

/// <summary>
/// Registers system-wide hotkeys via Win32 RegisterHotKey on a hidden message-only
/// window and raises <see cref="QuickPasteRequested"/> when the combo is pressed.
/// MVP registers a single Quick Paste combo (Ctrl+Shift+V).
/// </summary>
public sealed partial class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int QuickPasteId = 1;

    // Modifier + virtual-key codes.
    private const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_NOREPEAT = 0x4000;
    private const uint VK_V = 0x56;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private HwndSource? _source;
    private bool _registered;

    /// <summary>Raised (on the UI thread) when the Quick Paste hotkey fires.</summary>
    public event EventHandler? QuickPasteRequested;

    public bool IsRegistered => _registered;

    public void Start()
    {
        if (_source is not null) return;

        var parameters = new HwndSourceParameters("ClipForgeHotkeys")
        {
            ParentWindow = HWND_MESSAGE,
            Width = 0,
            Height = 0
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);

        _registered = RegisterHotKey(
            _source.Handle, QuickPasteId,
            MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_V);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == QuickPasteId)
        {
            QuickPasteRequested?.Invoke(this, EventArgs.Empty);
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
                UnregisterHotKey(_source.Handle, QuickPasteId);
                _registered = false;
            }
            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hwnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hwnd, int id);
}
