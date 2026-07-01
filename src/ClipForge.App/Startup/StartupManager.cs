using System.Diagnostics;
using Microsoft.Win32;

namespace ClipForge.App.Startup;

/// <summary>
/// Applies the "Launch on Windows startup" preference by writing the app path to
/// the per-user Run key (HKCU\...\Run). No admin rights required.
/// </summary>
public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClipForge";

    public static void Apply(bool launchOnStartup)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key is null) return;

            if (launchOnStartup)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(ValueName, $"\"{exePath}\"");
            }
            else if (key.GetValue(ValueName) is not null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Registry access can be denied by policy; startup preference is best-effort.
        }
    }
}
