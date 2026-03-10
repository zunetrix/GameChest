using System;
using System.Collections.Generic;

namespace GameChest;

public enum DeathRollPhase { Idle, InProgress, Done }

public sealed record DeathRollEntry(string PlayerName, int Result, int OutOf, DateTime At);

public sealed class DeathRollState : IGameState {
    public DeathRollPhase Phase { get; set; } = DeathRollPhase.Idle;
    public bool IsActive => Phase == DeathRollPhase.InProgress;

    public List<DeathRollEntry> Chain { get; } = new();
    public string? Winner { get; set; }
    public string? Loser { get; set; }

    public void Reset() {
        Phase = DeathRollPhase.Idle;
        Chain.Clear();
        Winner = null;
        Loser = null;
    }
}
