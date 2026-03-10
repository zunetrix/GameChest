using System.Collections.Generic;

namespace GameChest;

public static class PrizeRollPhraseCategories {
    public const string GameStart = "GameStart";
    public const string NewBestRoll = "NewBestRoll";
    public const string GameEnd = "GameEnd";

    public static readonly IReadOnlyList<PhraseCategoryMeta> All = new List<PhraseCategoryMeta> {
        new(GameStart, "Game Start", new[] { "{max}", "{mode}" }, new[] {
            "The prize roll has begun! Roll your best with /random {max}.",
            "Step up and roll! The prize goes to the {mode}. Use /random {max}.",
            "The game is open! Roll with /random {max} and claim your place on the board.",
        }),
        new(NewBestRoll, "New Best Roll", new[] { "{player}", "{roll}", "{previous}" }, new[] {
            "{player} takes the lead with {roll}!",
            "{player} surges ahead with a {roll}!",
            "New top score: {player} with {roll}!",
        }),
        new(GameEnd, "Game End", new[] { "{winner}", "{roll}" }, new[] {
            "{winner} wins with a roll of {roll}!",
            "The prize goes to {winner} - final roll: {roll}.",
            "{winner} takes it all with {roll}!",
        }),
    };
}
