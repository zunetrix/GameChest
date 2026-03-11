using System;
using System.Collections.Generic;

namespace GameChest;

public enum DiceRoyalePhase { Idle, Registration, Rolling, PendingElimination, Done }

public record DiceRoyaleResult(string Winner, int PlayerCount, DateTime PlayedAt);

public sealed class DiceRoyaleState : IGameState {
    public DiceRoyalePhase Phase { get; set; } = DiceRoyalePhase.Idle;
    public bool IsActive => Phase is DiceRoyalePhase.Registration or DiceRoyalePhase.Rolling or DiceRoyalePhase.PendingElimination;
    public List<string> Players { get; } = new();
    public Dictionary<string, int> CurrentRoundRolls { get; } = new();
    public int Round { get; set; } = 0;
    public string? Winner { get; set; }
    // Players with 91-100 roll who get to eliminate someone
    public Queue<string> PendingEliminators { get; } = new();
    public string? CurrentEliminator { get; set; }

    public void Reset() {
        Phase = DiceRoyalePhase.Idle;
        Players.Clear();
        CurrentRoundRolls.Clear();
        Round = 0;
        Winner = null;
        PendingEliminators.Clear();
        CurrentEliminator = null;
    }

    public void ResetRound() {
        CurrentRoundRolls.Clear();
        PendingEliminators.Clear();
        CurrentEliminator = null;
    }
}
