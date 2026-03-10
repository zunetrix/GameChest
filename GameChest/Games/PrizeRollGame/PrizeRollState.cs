using System;

namespace GameChest;

public sealed class PrizeRollState : IGameState {
    public bool IsActive { get; private set; }
    public ParticipantList Participants { get; } = new();

    public DateTime? TimerEndsAt { get; set; }
    public bool IsTimerRunning => TimerEndsAt.HasValue;
    public TimeSpan TimeRemaining => TimerEndsAt.HasValue
        ? (TimerEndsAt.Value - DateTime.Now).Duration()
        : TimeSpan.Zero;

    public void Start() => IsActive = true;

    public void Reset() {
        IsActive = false;
        TimerEndsAt = null;
        Participants.Clear();
    }
}
