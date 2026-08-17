namespace DesktopPet.Pet;

/// <summary>
/// One animation: a horizontal sprite sheet plus how fast to play it. ticksPerFrame is
/// counted in host timer ticks (see PetWindow.TickMs), so leg cycles and lazy breathing
/// can share a single timer.
/// </summary>
/// <param name="FileName">Sheet name under the *executable's* Assets/Sprites folder.</param>
public sealed record SpriteSheet(string FileName, int FrameCount, int TicksPerFrame)
{
    /// <summary>How many ticks one full pass through the sheet takes.</summary>
    public int LoopTicks => FrameCount * TicksPerFrame;
}

/// <summary>
/// Everything that differs between pets. The window, state machine and animator are
/// shared: adding a pet means adding a profile plus its sprite sheets, then a thin exe
/// project that shows a PetWindow for that profile (see src/DesktopPet, src/DesktopDog).
/// </summary>
public sealed record PetProfile
{
    public required string DisplayName { get; init; }

    /// <summary>
    /// File name (no extension) of this pet's executable. StartupManager refuses to
    /// register launch-at-login unless the running process matches, so "dotnet run"
    /// can't point the Run key at dotnet.exe.
    /// </summary>
    public required string ExeName { get; init; }

    /// <summary>HKCU Run value name — distinct per pet so each can autostart on its own.</summary>
    public required string StartupValueName { get; init; }

    /// <summary>Size of a single frame; every sheet for a pet must share it.</summary>
    public required int FrameWidth { get; init; }
    public required int FrameHeight { get; init; }

    public required SpriteSheet Idle { get; init; }
    public required SpriteSheet Walk { get; init; }

    /// <summary>Optional one-shot flourish played between idles (the dog's howl). Null = never.</summary>
    public SpriteSheet? Special { get; init; }

    /// <summary>Chance that an idle spell ends in <see cref="Special"/> instead of a walk.</summary>
    public double SpecialChance { get; init; }

    /// <summary>Pixels to sink the pet's feet below the taskbar's top edge; negative lifts it.</summary>
    public int FeetSinkIntoTaskbar { get; init; }

    public double WalkSpeedPxPerTick { get; init; } = 3.0;

    public static PetProfile Crocodile { get; } = new()
    {
        DisplayName = "Desktop Pet Crocodile",
        ExeName = "DesktopPet",
        StartupValueName = "DesktopPetCrocodile",
        FrameWidth = 191,
        FrameHeight = 64,
        Idle = new SpriteSheet("croc_idle.png", FrameCount: 4, TicksPerFrame: 7),
        Walk = new SpriteSheet("croc_walk.png", FrameCount: 4, TicksPerFrame: 2),
    };

    public static PetProfile Dog { get; } = new()
    {
        DisplayName = "Desktop Pet Dog",
        ExeName = "DesktopDog",
        StartupValueName = "DesktopPetDog",
        FrameWidth = 78,
        FrameHeight = 64,
        // Two-frame pant (mouth shut -> tongue out), held long enough to read as breathing.
        Idle = new SpriteSheet("dog_idle.png", FrameCount: 2, TicksPerFrame: 6),
        Walk = new SpriteSheet("dog_walk.png", FrameCount: 4, TicksPerFrame: 2),
        // Head lifts, holds the howl, settles back — plays through exactly once.
        Special = new SpriteSheet("dog_howl.png", FrameCount: 6, TicksPerFrame: 3),
        SpecialChance = 0.3,
        WalkSpeedPxPerTick = 2.5,
    };
}
