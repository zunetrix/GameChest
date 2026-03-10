using System;
using System.Collections.Generic;

namespace GameChest;

public static class DeathRollTournamentPhraseCategories {
    public const string RegistrationOpen = "RegistrationOpen";
    public const string MatchStart = "MatchStart";
    public const string MatchEnd = "MatchEnd";
    public const string TournamentEnd = "TournamentEnd";
    public const string TournamentCanceled = "TournamentCanceled";

    public static readonly IReadOnlyList<PhraseCategoryMeta> All = new List<PhraseCategoryMeta> {
        new(RegistrationOpen, "Registration Open", Array.Empty<string>(), new[] {
            "Tournament registration is open! Use /random to sign up.",
            "Sign up for the tournament now! Roll /random to register.",
            "The tournament is accepting participants! Use /random to join.",
        }),
        new(MatchStart, "Match Start", new[] { "{player1}", "{player2}", "{round}", "{max}" }, new[] {
            "Round {round} match: {player1} vs {player2}! First roll - /random {max}.",
            "{player1} faces {player2} in Round {round}! Begin with /random {max}.",
            "Next up: {player1} vs {player2} (Round {round}). /random {max} to start!",
        }),
        new(MatchEnd, "Match End", new[] { "{winner}", "{loser}", "{round}" }, new[] {
            "{winner} advances! {loser} is eliminated in Round {round}.",
            "Round {round} over - {winner} moves on, {loser} is out!",
            "{loser} rolled 1. {winner} wins Round {round}!",
        }),
        new(TournamentEnd, "Tournament End", new[] { "{winner}" }, new[] {
            "{winner} is the tournament champion!",
            "The tournament is over - all hail the champion: {winner}!",
            "{winner} wins the tournament!",
        }),
        new(TournamentCanceled, "Tournament Canceled", Array.Empty<string>(), new[] {
            "The tournament has been canceled.",
            "Tournament stopped.",
        }),
    };
}
