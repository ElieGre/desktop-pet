namespace DesktopPet.Pet;

public enum PetState
{
    Idle,
    Walk,
    Special,
}

public enum FacingDirection
{
    Left = -1,
    Right = 1,
}

/// <summary>
/// Drives the pet's Idle &lt;-&gt; Walk loop, plus an optional Special spell (the dog's
/// howl) that an idle occasionally ends in instead of a walk. Advanced once per
/// DispatcherTimer tick; X is clamped to whatever bounds the caller passes in (the
/// current taskbar width).
/// </summary>
public sealed class PetStateMachine
{
    private static readonly Random Rng = new();

    private const int MinIdleTicks = 15;
    private const int MaxIdleTicks = 40;
    private const int MinWalkTicks = 15;
    private const int MaxWalkTicks = 60;

    private readonly double _walkSpeedPxPerTick;
    private readonly int _specialTicks;
    private readonly double _specialChance;

    private int _ticksRemaining;

    public PetState State { get; private set; } = PetState.Idle;
    public FacingDirection Facing { get; private set; } = FacingDirection.Right;
    public double X { get; private set; }

    /// <param name="specialTicks">
    /// How long a Special spell lasts. Pass the special sheet's LoopTicks so the
    /// animation plays through exactly once rather than looping mid-pose.
    /// </param>
    public PetStateMachine(double startX, double walkSpeedPxPerTick, int specialTicks = 0, double specialChance = 0)
    {
        X = startX;
        _walkSpeedPxPerTick = walkSpeedPxPerTick;
        _specialTicks = specialTicks;
        _specialChance = specialTicks > 0 ? specialChance : 0;
        _ticksRemaining = RandomTicks(MinIdleTicks, MaxIdleTicks);
    }

    public void Tick(double minX, double maxX)
    {
        _ticksRemaining--;

        if (State == PetState.Walk)
        {
            X += (int)Facing * _walkSpeedPxPerTick;

            if (X <= minX)
            {
                X = minX;
                Facing = FacingDirection.Right;
            }
            else if (X >= maxX)
            {
                X = maxX;
                Facing = FacingDirection.Left;
            }
        }

        if (_ticksRemaining <= 0)
            TransitionToNextState();
    }

    private void TransitionToNextState()
    {
        switch (State)
        {
            // Only an idle leads into the special, so the pet is already standing still
            // in the pose the special animation starts from.
            case PetState.Idle when Rng.NextDouble() < _specialChance:
                State = PetState.Special;
                _ticksRemaining = _specialTicks;
                break;

            case PetState.Idle:
                State = PetState.Walk;
                Facing = Rng.Next(2) == 0 ? FacingDirection.Left : FacingDirection.Right;
                _ticksRemaining = RandomTicks(MinWalkTicks, MaxWalkTicks);
                break;

            default:
                State = PetState.Idle;
                _ticksRemaining = RandomTicks(MinIdleTicks, MaxIdleTicks);
                break;
        }
    }

    private static int RandomTicks(int min, int max) => Rng.Next(min, max);
}
