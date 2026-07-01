using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ClipForge.Infrastructure.WindowsApi;

/// <summary>Win32 P/Invoke surface used for clipboard listening and source tracking.</summary>
[SupportedOSPlatform("windows")]
internal static partial class NativeMethods
{
    public const int WM_CLIPBOARDUPDATE = 0x031D;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AddClipboardFormatListener(IntPtr hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RemoveClipboardFormatListener(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GetWindowTextW(IntPtr hwnd, [Out] char[] text, int maxCount);

    [LibraryImport("user32.dll")]
    public static partial int GetWindowTextLengthW(IntPtr hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
}
