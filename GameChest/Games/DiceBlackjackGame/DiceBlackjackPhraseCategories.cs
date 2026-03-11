using System;

namespace GameChest;

public static class DiceBlackjackPhraseCategories {
    public const string RegistrationOpen = "RegistrationOpen";
    public const string PlayerTurn       = "PlayerTurn";
    public const string PlayerDealt      = "PlayerDealt";
    public const string PlayerHit        = "PlayerHit";
    public const string PlayerBust       = "PlayerBust";
    public const string PlayerStand      = "PlayerStand";
    public const string DealerDraw       = "DealerDraw";
    public const string DealerBust       = "DealerBust";
    public const string DealerStand      = "DealerStand";
    public const string PlayerWin        = "PlayerWin";
    public const string PlayerLoss       = "PlayerLoss";
    public const string PlayerPush       = "PlayerPush";
    public const string GameEnd          = "GameEnd";
    public const string GameCanceled     = "GameCanceled";

    public static readonly PhraseCategoryMeta[] All = [
        new(RegistrationOpen, "Registration Open",  new[] { "maxroll" },                        new[] { "Blackjack is open! Roll /random {maxroll} to join!" }),
        new(PlayerTurn,       "Player Turn",        new[] { "player", "maxroll" },              new[] { "It's {player}'s turn! Roll /random {maxroll} twice for your starting hand." }),
        new(PlayerDealt,      "Player Dealt",       new[] { "player", "card", "total" },        new[] { "{player} is dealt {card}. Hand: {total}" }),
        new(PlayerHit,        "Player Hit",         new[] { "player", "card", "total" },        new[] { "{player} hits and gets {card}! Total: {total}" }),
        new(PlayerBust,       "Player Bust",        new[] { "player", "total" },                new[] { "{player} busts with {total}!" }),
        new(PlayerStand,      "Player Stand",       new[] { "player", "total" },                new[] { "{player} stands with {total}." }),
        new(DealerDraw,       "Dealer Draw",        new[] { "card", "total" },                  new[] { "Dealer draws {card}. Total: {total}" }),
        new(DealerBust,       "Dealer Bust",        new[] { "total" },                          new[] { "Dealer busts with {total}! All standing players win!" }),
        new(DealerStand,      "Dealer Stand",       new[] { "total" },                          new[] { "Dealer stands with {total}." }),
        new(PlayerWin,        "Player Win",         new[] { "player", "score" },                new[] { "{player} wins with {score}!" }),
        new(PlayerLoss,       "Player Loss",        new[] { "player", "score" },                new[] { "{player} loses with {score}." }),
        new(PlayerPush,       "Player Push",        new[] { "player", "score" },                new[] { "{player} ties the dealer with {score}." }),
        new(GameEnd,          "Game End",           new[] { "winner", "score" },                new[] { "{winner} is the champion with {score}!" }),
        new(GameCanceled,     "Game Canceled",      Array.Empty<string>(),                      new[] { "Blackjack canceled." }),
    ];
}
