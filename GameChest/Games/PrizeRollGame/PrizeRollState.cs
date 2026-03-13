using System;

namespace GameChest;

public enum PrizeRollPhase { Idle, Active, Finished }

public sealed class PrizeRollState : IGameState {
    public PrizeRollPhase Phase { get; private set; } = PrizeRollPhase.Idle;
    public bool IsActive => Phase == PrizeRollPhase.Active;
    public ParticipantList Participants { get; } = new();

    public DateTime? TimerEndsAt { get; set; }
    public bool IsTimerRunning => TimerEndsAt.HasValue;
    public TimeSpan TimeRemaining => TimerEndsAt.HasValue
        ? (TimerEndsAt.Value - DateTime.Now).Duration()
        : TimeSpan.Zero;

    public void Start() => Phase = PrizeRollPhase.Active;

    public void Reset() {
        Phase = PrizeRollPhase.Idle;
        TimerEndsAt = null;
        Participants.Clear();
    }

    GamePhase IGameState.Phase => Phase.ToGamePhase();
}
