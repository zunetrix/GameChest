using System;
using System.Collections.Generic;

namespace GameChest;

public static class DiceRoyalePhraseCategories {
    public const string RegistrationOpen = "RegistrationOpen";
    public const string RoundStart = "RoundStart";
    public const string PlayerEliminated = "PlayerEliminated";
    public const string PlayerSurvives = "PlayerSurvives";
    public const string PlayerAdvantage = "PlayerAdvantage";
    public const string PlayerEliminates = "PlayerEliminates";
    public const string GameEnd = "GameEnd";
    public const string GameCanceled = "GameCanceled";

    public static readonly IReadOnlyList<PhraseCategoryMeta> All = new List<PhraseCategoryMeta> {
        new(RegistrationOpen, "Registration Open", Array.Empty<string>(),           new[] { "The Dice Royale begins! Use /random to enter!" }),
        new(RoundStart,       "Round Start",        new[] { "{round}", "{maxroll}" }, new[] { "Round {round}! All players roll /random {maxroll}!" }),
        new(PlayerEliminated, "Player Eliminated",  new[] { "{player}", "{roll}" },   new[] { "{player} rolled {roll} (1-20) and is eliminated!" }),
        new(PlayerSurvives,   "Player Survives",    new[] { "{player}", "{roll}" },   new[] { "{player} rolled {roll} and survives this round." }, false),
        new(PlayerAdvantage,  "Player Advantage",   new[] { "{player}", "{roll}" },   new[] { "{player} rolled {roll} and gains an advantage!" }),
        new(PlayerEliminates, "Player Eliminates",  new[] { "{player}", "{roll}" },   new[] { "{player} rolled {roll} - a critical hit! They may eliminate a player!" }),
        new(GameEnd,          "Game End",           new[] { "{winner}" },             new[] { "{winner} is the last survivor of the Dice Royale!" }),
        new(GameCanceled,     "Game Canceled",      Array.Empty<string>(),            new[] { "The Dice Royale has been canceled." }, false),
    };
}
