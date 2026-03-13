using System;
using System.Collections.Generic;

namespace GameChest;

public enum TavernBrawlPhase { Idle, Registering, Rolling, PendingChoice, Finished }

public record TavernBrawlResult(string Winner, int PlayerCount, DateTime PlayedAt);

public sealed class TavernBrawlState : IGameState {
    public TavernBrawlPhase Phase { get; set; } = TavernBrawlPhase.Idle;
    public bool IsActive => Phase is TavernBrawlPhase.Registering or TavernBrawlPhase.Rolling or TavernBrawlPhase.PendingChoice;
    public List<string> Players { get; } = new();
    public Dictionary<string, int> CurrentRoundRolls { get; } = new();
    public int Round { get; set; } = 0;
    public string? Winner { get; set; }
    // PendingChoice: the highest roller gets to eliminate someone
    public string? HighestRoller { get; set; }
    public int HighestRoll { get; set; }
    public string? LowestRoller { get; set; }

    public void Reset() {
        Phase = TavernBrawlPhase.Idle;
        Players.Clear();
        CurrentRoundRolls.Clear();
        Round = 0;
        Winner = null;
        HighestRoller = null;
        HighestRoll = 0;
        LowestRoller = null;
    }

    public void ResetRound() {
        CurrentRoundRolls.Clear();
        HighestRoller = null;
        HighestRoll = 0;
        LowestRoller = null;
    }

    GamePhase IGameState.Phase => Phase.ToGamePhase();
}
