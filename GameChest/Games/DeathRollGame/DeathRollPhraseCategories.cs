using System.Collections.Generic;

namespace GameChest;

public static class DeathRollPhraseCategories {
    public const string GameStart = "GameStart";
    public const string GameEnd = "GameEnd";
    public const string GameCanceled = "GameCanceled";

    public static readonly IReadOnlyList<PhraseCategoryMeta> All = new List<PhraseCategoryMeta> {
        new(GameStart, "Game Start", new[] { "{max}" }, new[] {
            "Death Roll has begun! First player rolls /random {max}.",
            "Let the Death Roll begin! Use /random {max} to start the chain.",
            "Who will survive? First roll goes - /random {max}!",
        }),
        new(GameEnd, "Round End", new[] { "{winner}", "{loser}" }, new[] {
            "{winner} wins! {loser} rolled 1 and is out!",
            "The chain ends here. {loser} rolled 1 - {winner} takes the round!",
            "{loser} falls! {winner} survives the Death Roll!",
        }),
        new(GameCanceled, "Game Canceled", System.Array.Empty<string>(), new[] {
            "Death Roll canceled.",
            "The round has been called off.",
        }),
    };
}
