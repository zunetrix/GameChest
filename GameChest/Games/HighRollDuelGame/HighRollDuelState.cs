using System;
using System.Collections.Generic;

namespace GameChest;

public enum HighRollDuelPhase { Idle, Registration, Rolling, Done }

public record HighRollDuelResult(string Winner, int PlayerCount, DateTime PlayedAt);

public sealed class HighRollDuelState : IGameState {
    public HighRollDuelPhase Phase { get; set; } = HighRollDuelPhase.Idle;
    public bool IsActive => Phase is HighRollDuelPhase.Registration or HighRollDuelPhase.Rolling;
    public List<string> Players { get; } = new();
    public Dictionary<string, int> CurrentRoundRolls { get; } = new();
    public int Round { get; set; } = 0;
    public string? Winner { get; set; }
    public List<string> RoundEliminations { get; } = new();

    public void Reset() {
        Phase = HighRollDuelPhase.Idle;
        Players.Clear();
        CurrentRoundRolls.Clear();
        Round = 0;
        Winner = null;
        RoundEliminations.Clear();
    }

    public void ResetRound() {
        CurrentRoundRolls.Clear();
        RoundEliminations.Clear();
    }
}
