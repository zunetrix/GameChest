using System;
using System.Collections.Generic;

namespace GameChest;

public enum KingOfTheHillPhase { Idle, Registration, Rolling, Done }

public record KingOfTheHillResult(string Winner, int Rounds, DateTime PlayedAt);

public sealed class KingOfTheHillState : IGameState {
    public KingOfTheHillPhase Phase { get; set; } = KingOfTheHillPhase.Idle;
    public bool IsActive => Phase is KingOfTheHillPhase.Registration or KingOfTheHillPhase.Rolling;
    public List<string> Players { get; } = new();
    public Dictionary<string, int> CurrentRoundRolls { get; } = new();
    public string? King { get; set; }
    public int KingHoldCount { get; set; } = 0;
    public int Round { get; set; } = 0;
    public string? Winner { get; set; }

    public void Reset() {
        Phase = KingOfTheHillPhase.Idle;
        Players.Clear();
        CurrentRoundRolls.Clear();
        King = null;
        KingHoldCount = 0;
        Round = 0;
        Winner = null;
    }

    public void ResetRound() => CurrentRoundRolls.Clear();
}
