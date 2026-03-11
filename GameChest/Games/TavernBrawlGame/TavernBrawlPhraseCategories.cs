using System;
using System.Collections.Generic;

namespace GameChest;

public static class TavernBrawlPhraseCategories {
    public const string RegistrationOpen = "RegistrationOpen";
    public const string RoundStart = "RoundStart";
    public const string KnockedOut = "KnockedOut";
    public const string HighestChooses = "HighestChooses";
    public const string GameEnd = "GameEnd";
    public const string GameCanceled = "GameCanceled";

    public static readonly IReadOnlyList<PhraseCategoryMeta> All = new List<PhraseCategoryMeta> {
        new(RegistrationOpen, "Registration Open", Array.Empty<string>(),             new[] { "The Tavern Brawl is starting! Use /random to join!" }),
        new(RoundStart,       "Round Start",        new[] { "{round}", "{maxroll}" }, new[] { "Round {round}! Everyone roll /random {maxroll}!" }),
        new(KnockedOut,       "Knocked Out",        new[] { "{player}", "{roll}" },   new[] { "{player} rolled {roll} and is knocked out!" }),
        new(HighestChooses,   "Highest Chooses",    new[] { "{player}", "{roll}" },   new[] { "{player} rolled {roll} - the highest! They get to knock out another fighter!" }),
        new(GameEnd,          "Game End",           new[] { "{winner}" },             new[] { "{winner} stands victorious! The Tavern Brawl is over!" }),
        new(GameCanceled,     "Game Canceled",      Array.Empty<string>(),            new[] { "The Tavern Brawl has been canceled." }, false),
    };
}
