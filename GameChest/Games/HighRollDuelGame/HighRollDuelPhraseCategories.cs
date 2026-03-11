using System;
using System.Collections.Generic;

namespace GameChest;

public static class HighRollDuelPhraseCategories {
    public const string RegistrationOpen = "RegistrationOpen";
    public const string RoundStart = "RoundStart";
    public const string PlayerEliminated = "PlayerEliminated";
    public const string GameEnd = "GameEnd";
    public const string GameCanceled = "GameCanceled";

    public static readonly IReadOnlyList<PhraseCategoryMeta> All = new List<PhraseCategoryMeta> {
        new(RegistrationOpen, "Registration Open", Array.Empty<string>(),             new[] { "Registration is open! Use /random to join the High Roll Duel!" }),
        new(RoundStart,       "Round Start",        new[] { "{round}", "{maxroll}" }, new[] { "Round {round} begins! All players roll /random {maxroll}!" }),
        new(PlayerEliminated, "Player Eliminated",  new[] { "{player}", "{roll}" },   new[] { "{player} rolled {roll} and has been eliminated!" }),
        new(GameEnd,          "Game End",           new[] { "{winner}" },             new[] { "{winner} is the last one standing and wins the High Roll Duel!" }),
        new(GameCanceled,     "Game Canceled",      Array.Empty<string>(),            new[] { "The High Roll Duel has been canceled." }, false),
    };
}
