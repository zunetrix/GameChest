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

    private IEnumerable<IGame> AllGames => [FightGame, PrizeRollGame, DeathRollGame, DeathRollTournamentGame, WordGuessGame];
    public bool AnyGameActive => AllGames.Any(g => g.IsActive);

    public GameManager(Plugin plugin) {
        FightGame = new FightGame(plugin);
        PrizeRollGame = new PrizeRollGame(plugin);
        DeathRollGame = new DeathRollGame(plugin);
        DeathRollTournamentGame = new DeathRollTournamentGame(plugin);
        WordGuessGame = new WordGuessGame(plugin);
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

