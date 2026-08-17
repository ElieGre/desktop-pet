namespace DesktopPet.Pet;

public enum PetState
{
    Idle,
    Walk,
}

public enum FacingDirection
{
    Left = -1,
    Right = 1,
}

/// <summary>
/// Drives the croc's Idle &lt;-&gt; Walk loop. Advanced once per DispatcherTimer tick;
/// X is clamped to whatever bounds the caller passes in (the current taskbar width).
/// </summary>
public sealed class PetStateMachine
{
    private static readonly Random Rng = new();

    private const double WalkSpeedPxPerTick = 3.0;
    private const int MinIdleTicks = 15;
    private const int MaxIdleTicks = 40;
    private const int MinWalkTicks = 15;
    private const int MaxWalkTicks = 60;

    private int _ticksRemaining;

    public PetState State { get; private set; } = PetState.Idle;
    public FacingDirection Facing { get; private set; } = FacingDirection.Right;
    public double X { get; private set; }

    public PetStateMachine(double startX)
    {
        X = startX;
        _ticksRemaining = RandomTicks(MinIdleTicks, MaxIdleTicks);
    }

    public void Tick(double minX, double maxX)
    {
        _ticksRemaining--;

        if (State == PetState.Walk)
        {
            X += (int)Facing * WalkSpeedPxPerTick;

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
        if (State == PetState.Idle)
        {
            State = PetState.Walk;
            Facing = Rng.Next(2) == 0 ? FacingDirection.Left : FacingDirection.Right;
            _ticksRemaining = RandomTicks(MinWalkTicks, MaxWalkTicks);
        }
        else
        {
            State = PetState.Idle;
            _ticksRemaining = RandomTicks(MinIdleTicks, MaxIdleTicks);
        }
    }

    private static int RandomTicks(int min, int max) => Rng.Next(min, max);
}
