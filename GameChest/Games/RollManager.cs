using System;

using GameChest.Extensions;

namespace GameChest;

public class RollManager {
    private readonly Plugin Plugin;

    public RollManager(Plugin plugin) {
        Plugin = plugin;
    }

    public void ProcessIncomingRollMessage(string fullName, int result, int outOf) {
        if (!Plugin.GameManager.AnyGameActive)
            return;

        if (Plugin.Config.IsBlockListActive && Plugin.Config.Blocklist.ContainsPlayer(fullName)) {
            foreach (var game in new IGame[] { Plugin.GameManager.FightGame, Plugin.GameManager.PrizeRollGame, Plugin.GameManager.DeathRollGame, Plugin.GameManager.DeathRollTournamentGame })
                if (game.IsActive) game.Notification.ShowError($"Player {fullName} attempted to roll but is on the blocklist");
            return;
        }

        var roll = new Roll(fullName, result, outOf);

        DalamudApi.PluginLog.Debug($"Roll: {fullName} [{result}/{outOf}]");
        Plugin.GameManager.ProcessRoll(roll);
    }
}

public class Roll {
    public string PlayerName;
    public int Result;
    public int OutOf;
    public DateTime At { get; } = DateTime.Now;

    private Roll(string name) {
        PlayerName = name;
    }

    public Roll(string fullName, int roll, int outOf) {
        PlayerName = fullName;
        Result = roll;
        OutOf = outOf != 0 ? outOf : -1;
    }

    public static Roll Dummy(string name = "Unknown") => new(name);
}
