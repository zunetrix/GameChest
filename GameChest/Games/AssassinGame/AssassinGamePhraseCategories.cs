using System;
using System.Collections.Generic;

namespace GameChest;

public static class AssassinGamePhraseCategories {
    public const string RegistrationOpen = "RegistrationOpen";
    public const string GameStart = "GameStart";
    public const string AttackAttempt = "AttackAttempt";
    public const string AssassinationSuccess = "AssassinationSuccess";
    public const string AssassinationFailed = "AssassinationFailed";
    public const string PlayerEliminated = "PlayerEliminated";
    public const string GameEnd = "GameEnd";
    public const string GameCanceled = "GameCanceled";

    public static readonly IReadOnlyList<PhraseCategoryMeta> All = new List<PhraseCategoryMeta> {
        new(RegistrationOpen,     "Registration Open",     Array.Empty<string>(),                                         new[] { "The Assassin Game begins! Use /random to join. Targets will be assigned!" }),
        new(GameStart,            "Game Start",            Array.Empty<string>(),                                         new[] { "Targets have been assigned. The hunt begins... trust no one." }),
        new(AttackAttempt,        "Attack Attempt",        new[] { "{maxroll}" },                                         new[] { "An assassination is underway! Attacker and defender, roll /random {maxroll}!" }),
        new(AssassinationSuccess, "Assassination Success", new[] { "{attacker}", "{aroll}", "{defender}", "{droll}" },    new[] { "{attacker} ({aroll}) eliminated {defender} ({droll})!" }),
        new(AssassinationFailed,  "Assassination Failed",  new[] { "{attacker}", "{aroll}", "{defender}", "{droll}" },    new[] { "{defender} ({droll}) survived the attack from {attacker} ({aroll})!" }),
        new(PlayerEliminated,     "Player Eliminated",     new[] { "{player}", "{remaining}" },                           new[] { "{player} has been eliminated. {remaining} players remain." }, false),
        new(GameEnd,              "Game End",              new[] { "{winner}" },                                           new[] { "{winner} is the last survivor - the ultimate assassin!" }),
        new(GameCanceled,         "Game Canceled",         Array.Empty<string>(),                                          new[] { "The Assassin Game has been canceled." }, false),
    };
}
