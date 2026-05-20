using Microsoft.Win32;

namespace DisplayConfigManager.Services;

/// <summary>
/// Manages the HKCU "Start with Windows" registry entry so the tray app
/// re-applies the default preset after a reboot.
/// </summary>
public static class StartupRegistrationService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DisplayConfigManager";

    public static bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is string s && !string.IsNullOrWhiteSpace(s);
    }

    public static void SetRegistered(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKey, writable: true);

        if (key is null) return;

        if (enable)
        {
            var exePath = Environment.ProcessPath
                ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
                key.SetValue(ValueName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
