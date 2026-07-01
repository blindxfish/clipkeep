using System.Diagnostics;
using System.Runtime.Versioning;

namespace ClipForge.Infrastructure.WindowsApi;

/// <summary>Captured identity of the app that owned the foreground at copy time.</summary>
public readonly record struct SourceInfo(string? ProcessName, string? AppName, string? WindowTitle);

/// <summary>
/// Reads the active window's process and title via Win32, so entries can later be
/// searched by originating application/context.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ForegroundWindowTracker
{
    public SourceInfo Capture()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return default;

        var title = ReadWindowTitle(hwnd);

        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        string? processName = null;
        string? appName = null;
        if (pid != 0)
        {
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;                       // e.g. "chrome"
                appName = SafeMainModuleName(proc) ?? processName;    // e.g. "chrome.exe"
            }
            catch
            {
                // Process may have exited or be inaccessible; leave nulls.
            }
        }

        return new SourceInfo(processName, appName, title);
    }

    private static string ReadWindowTitle(IntPtr hwnd)
    {
        var len = NativeMethods.GetWindowTextLengthW(hwnd);
        if (len <= 0) return string.Empty;
        var buffer = new char[len + 1];
        var copied = NativeMethods.GetWindowTextW(hwnd, buffer, buffer.Length);
        return new string(buffer, 0, copied);
    }

    private static string? SafeMainModuleName(Process proc)
    {
        try { return proc.MainModule?.ModuleName; }
        catch { return null; } // access denied for elevated/other-user processes
    }
}
