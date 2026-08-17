using System.IO;
using Microsoft.Win32;

namespace DesktopPet.Native;

/// <summary>
/// Toggles launch-at-login via the per-user HKCU Run key — no admin rights needed,
/// and it's removed automatically when the app uninstalls (nothing else touches the key).
/// Only works when running the built DesktopPet.exe directly: "dotnet run" launches the
/// app through dotnet.exe, and pointing the Run key at that would relaunch dotnet.exe
/// with no arguments instead of the pet.
/// </summary>
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopPetCrocodile";

    public static bool CanManageStartup() =>
        string.Equals(Path.GetFileNameWithoutExtension(ExePath), "DesktopPet", StringComparison.OrdinalIgnoreCase);

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var existing = key?.GetValue(ValueName) as string;
        return existing != null && existing.Trim('"').Equals(ExePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
            key.SetValue(ValueName, $"\"{ExePath}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string ExePath =>
        Environment.ProcessPath ?? throw new InvalidOperationException("Could not resolve executable path.");
}
