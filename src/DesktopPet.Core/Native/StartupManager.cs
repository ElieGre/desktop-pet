using System.IO;
using DesktopPet.Pet;
using Microsoft.Win32;

namespace DesktopPet.Native;

/// <summary>
/// Toggles launch-at-login via the per-user HKCU Run key — no admin rights needed,
/// and it's removed automatically when the app uninstalls (nothing else touches the key).
/// Each pet gets its own value name, so the croc and the dog autostart independently.
/// Only works when running the built exe directly: "dotnet run" launches the app through
/// dotnet.exe, and pointing the Run key at that would relaunch dotnet.exe with no
/// arguments instead of the pet.
/// </summary>
public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool CanManageStartup(PetProfile profile) =>
        string.Equals(Path.GetFileNameWithoutExtension(ExePath), profile.ExeName, StringComparison.OrdinalIgnoreCase);

    public static bool IsEnabled(PetProfile profile)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var existing = key?.GetValue(profile.StartupValueName) as string;
        return existing != null && existing.Trim('"').Equals(ExePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(PetProfile profile, bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
            key.SetValue(profile.StartupValueName, $"\"{ExePath}\"");
        else
            key.DeleteValue(profile.StartupValueName, throwOnMissingValue: false);
    }

    private static string ExePath =>
        Environment.ProcessPath ?? throw new InvalidOperationException("Could not resolve executable path.");
}
