using System;
using System.Collections.Generic;
using System.Linq;

namespace GameChest;

public class GameManager : IDisposable {
    public FightGame FightGame { get; }
    public PrizeRollGame PrizeRollGame { get; }
    public DeathRollGame DeathRollGame { get; }
    public DeathRollTournamentGame DeathRollTournamentGame { get; }
    public WordGuessGame WordGuessGame { get; }
    public HighRollDuelGame HighRollDuelGame { get; }
    public TavernBrawlGame TavernBrawlGame { get; }
    public DiceRoyaleGame DiceRoyaleGame { get; }
    public KingOfTheHillGame KingOfTheHillGame { get; }
    public AssassinGame AssassinGame { get; }

    public IEnumerable<IGame> AllGames => [FightGame, PrizeRollGame, DeathRollGame, DeathRollTournamentGame, WordGuessGame, HighRollDuelGame, TavernBrawlGame, DiceRoyaleGame, KingOfTheHillGame, AssassinGame];
    public bool AnyGameActive => AllGames.Any(g => g.IsActive);

    public GameManager(Plugin plugin) {
        FightGame = new FightGame(plugin);
        PrizeRollGame = new PrizeRollGame(plugin);
        DeathRollGame = new DeathRollGame(plugin);
        DeathRollTournamentGame = new DeathRollTournamentGame(plugin);
        WordGuessGame = new WordGuessGame(plugin);
        HighRollDuelGame = new HighRollDuelGame(plugin);
        TavernBrawlGame = new TavernBrawlGame(plugin);
        DiceRoyaleGame = new DiceRoyaleGame(plugin);
        KingOfTheHillGame = new KingOfTheHillGame(plugin);
        AssassinGame = new AssassinGame(plugin);
    }

    public void ProcessRoll(Roll roll) {
        foreach (var game in AllGames)
            if (game.IsActive) game.ProcessRoll(roll);
    }

    public void ProcessChatMessage(string senderFullName, string message, Dalamud.Game.Text.XivChatType chatType) {
        foreach (var consumer in AllGames.OfType<IChatConsumer>())
            if (((IGame)consumer).IsActive)
                consumer.ProcessChatMessage(senderFullName, message, chatType);
    }

    public void Dispose() { }
}

