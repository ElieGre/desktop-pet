using System.Windows;

namespace DesktopPet.Pet;

/// <summary>
/// Resolves the taskbar's on-screen bounds from the primary monitor's work area, so the
/// pet knows where to stand and how far it can walk. DIP-safe (uses WPF's own
/// SystemParameters instead of raw Win32 rects, avoiding manual DPI conversion).
/// v1 assumes a single, non-auto-hidden taskbar on the primary monitor.
/// </summary>
public static class TaskbarTracker
{
    public static Rect GetBounds()
    {
        var work = SystemParameters.WorkArea;
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        // Bottom taskbar (the default Windows layout).
        if (work.Bottom < screenHeight)
            return new Rect(work.Left, work.Bottom, work.Width, screenHeight - work.Bottom);

        // Top taskbar.
        if (work.Top > 0)
            return new Rect(work.Left, 0, work.Width, work.Top);

        // Left/right taskbar or auto-hide: no discernible bar to stand on, fall back to
        // the bottom edge of the work area so the pet still has somewhere to walk.
        return new Rect(work.Left, work.Bottom - 1, work.Width, 1);
    }
}
