using System.Runtime.InteropServices;

namespace ClipForge.App.Interop;

/// <summary>
/// Captures the window that had focus before ClipForge's popup appeared, then
/// restores focus to it and synthesizes Ctrl+V so the chosen clip lands in the
/// app the user was actually working in.
/// </summary>
public static class ForegroundPaster
{
    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    /// <summary>Handle of the currently focused (foreground) window, or Zero.</summary>
    public static IntPtr CaptureForeground() => GetForegroundWindow();

    /// <summary>
    /// Bring <paramref name="target"/> to the foreground and send Ctrl+V. No-op if
    /// the handle is invalid. Best-effort: focus restoration can be refused by the OS.
    /// </summary>
    public static void PasteInto(IntPtr target)
    {
        if (target == IntPtr.Zero) return;
        SetForegroundWindow(target);

        var inputs = new INPUT[]
        {
            KeyInput(VK_CONTROL, down: true),
            KeyInput(VK_V, down: true),
            KeyInput(VK_V, down: false),
            KeyInput(VK_CONTROL, down: false),
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT KeyInput(ushort vk, bool down) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                dwFlags = down ? 0 : KEYEVENTF_KEYUP
            }
        }
    };

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    // The union must span its largest member (MOUSEINPUT) so Marshal.SizeOf<INPUT>()
    // equals the OS-expected size; SendInput rejects a mismatched cbSize otherwise.
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
