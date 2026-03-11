using System;
using System.Collections.Generic;

namespace GameChest;

public static class KingOfTheHillPhraseCategories {
    public const string RegistrationOpen = "RegistrationOpen";
    public const string RoundStart = "RoundStart";
    public const string NewKing = "NewKing";
    public const string KingDefends = "KingDefends";
    public const string GameEnd = "GameEnd";
    public const string GameCanceled = "GameCanceled";

    public static readonly IReadOnlyList<PhraseCategoryMeta> All = new List<PhraseCategoryMeta> {
        new(RegistrationOpen, "Registration Open", Array.Empty<string>(),                                               new[] { "King of the Hill starts! Use /random to join!" }),
        new(RoundStart,       "Round Start",        new[] { "{round}", "{maxroll}", "{king}", "{holds}", "{target}" }, new[] { "Round {round}! Challengers roll /random {maxroll} to claim the throne! King: {king} (held {holds}/{target})" }),
        new(NewKing,          "New King",           new[] { "{player}", "{roll}" },                                    new[] { "{player} rolled {roll} and takes the throne! All hail the new King!" }),
        new(KingDefends,      "King Defends",       new[] { "{king}", "{roll}", "{holds}", "{target}" },               new[] { "{king} rolled {roll} and defends the throne! ({holds}/{target} rounds held)" }),
        new(GameEnd,          "Game End",           new[] { "{winner}", "{holds}" },                                   new[] { "{winner} has held the throne for {holds} rounds and wins King of the Hill!" }),
        new(GameCanceled,     "Game Canceled",      Array.Empty<string>(),                                             new[] { "King of the Hill has been canceled." }, false),
    };
}
