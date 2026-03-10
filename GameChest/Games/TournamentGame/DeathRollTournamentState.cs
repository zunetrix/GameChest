using System.Collections.Generic;

namespace GameChest;

public enum DeathRollTournamentPhase { Idle, Registration, Preparing, Match, Done }

public sealed class DeathRollTournamentMatch {
    public const string PlaceholderPlayer = "John Doe (Placeholder)";

    public string Player1 { get; set; } = "";
    public string Player2 { get; set; } = "";
    public string? Winner { get; set; }
    public bool IsBye => Player1 == PlaceholderPlayer || Player2 == PlaceholderPlayer;
    public bool IsComplete => Winner != null;
}

public sealed class DeathRollTournamentRound {
    public int RoundNumber { get; set; }
    public List<DeathRollTournamentMatch> Matches { get; } = new();
}

public sealed class DeathRollTournamentState : IGameState {
    public DeathRollTournamentPhase Phase { get; set; } = DeathRollTournamentPhase.Idle;
    public bool IsActive => Phase != DeathRollTournamentPhase.Idle;

    // Registration
    public List<string> RegisteredPlayers { get; } = new();

    // Bracket
    public List<DeathRollTournamentRound> Rounds { get; } = new();
    public int CurrentRoundIndex { get; set; }
    public int CurrentMatchIndex { get; set; }

    // Current match
    public string? MatchPlayer1 { get; set; }
    public string? MatchPlayer2 { get; set; }
    public List<DeathRollEntry> MatchChain { get; } = new();
    public string? MatchWinner { get; set; }
    public int MatchStartingRoll { get; set; } = 999;

    // Final result
    public string? TournamentWinner { get; set; }

    public DeathRollTournamentRound? CurrentRound =>
        CurrentRoundIndex < Rounds.Count ? Rounds[CurrentRoundIndex] : null;

    public DeathRollTournamentMatch? CurrentMatch =>
        CurrentRound != null && CurrentMatchIndex < CurrentRound.Matches.Count
            ? CurrentRound.Matches[CurrentMatchIndex]
            : null;

    public void Reset() {
        Phase = DeathRollTournamentPhase.Idle;
        RegisteredPlayers.Clear();
        Rounds.Clear();
        CurrentRoundIndex = 0;
        CurrentMatchIndex = 0;
        MatchPlayer1 = null;
        MatchPlayer2 = null;
        MatchChain.Clear();
        MatchWinner = null;
        TournamentWinner = null;
    }
}
