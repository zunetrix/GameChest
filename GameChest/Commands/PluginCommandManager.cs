using System;
using System.Linq;

using Dalamud.Game.Command;

using GameChest.Util;

namespace GameChest;

public class PluginCommandManager : IDisposable {
    private Plugin Plugin { get; }
    private string? _customCommand;
    private readonly string _mainCommand = "/gamechest";

    public PluginCommandManager(Plugin plugin) {
        Plugin = plugin;

        DalamudApi.CommandManager.AddHandler(_mainCommand, new CommandInfo(OnMainCommand) {
            HelpMessage = """
            /gamechest -> Show / Hide UI
            prizeroll
            fightclub
            deathroll
            deathrolltournament
            wordguess
            """,
        });

        RegisterCustomCommand(Plugin.Config.CustomCommand);
    }

    public void SetCustomCommand(string newCommand) {
        newCommand = newCommand.Trim().ToLowerInvariant();
        if (!newCommand.StartsWith('/') || newCommand.Length < 2) return;
        if (newCommand == _customCommand) return;
        if (newCommand == _mainCommand) return;

        UnregisterCustomCommand();
        RegisterCustomCommand(newCommand);
        Plugin.Config.CustomCommand = newCommand;
        Plugin.Config.Save();
    }

    public void ClearCustomCommand() {
        UnregisterCustomCommand();
        Plugin.Config.CustomCommand = string.Empty;
        Plugin.Config.Save();
    }

    private void RegisterCustomCommand(string command) {
        command = command.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(command) || !command.StartsWith('/') || command.Length < 2) return;
        if (command == _mainCommand) return;

        try {
            DalamudApi.CommandManager.AddHandler(command, new CommandInfo(OnMainCommand) {
                HelpMessage = "Alias command",
            });
            _customCommand = command;
        } catch (Exception ex) {
            DalamudApi.PluginLog.Warning(ex, $"Failed to register command: {command}");
        }
    }

    private void UnregisterCustomCommand() {
        if (_customCommand == null) return;
        DalamudApi.CommandManager.RemoveHandler(_customCommand);
        _customCommand = null;
    }

    public void Dispose() {
        DalamudApi.CommandManager.RemoveHandler(_mainCommand);
        UnregisterCustomCommand();
    }

    private void OnMainCommand(string command, string arguments) {
        var parsedArgs = ArgumentParser.ParseCommandArgs(arguments);

        if (parsedArgs.Any()) {
            var subcommand = parsedArgs[0];
            switch (subcommand) {
                case "settings":
                    Plugin.Ui.SettingsWindow.Toggle();
                    break;
                case "prizeroll":
                    Plugin.Ui.PrizeRollWindow.Toggle();
                    break;
                case "fightclub":
                    Plugin.Ui.FightGameWindow.Toggle();
                    break;
                case "deathroll":
                    Plugin.Ui.DeathRollWindow.Toggle();
                    break;
                case "deathrolltournament":
                    Plugin.Ui.DeathRollTournamentWindow.Toggle();
                    break;
                case "wordguess":
                    Plugin.Ui.WordGuessWindow.Toggle();
                    break;
                default:
                    DalamudApi.ChatGui.PrintError($"Unrecognized subcommand: '{subcommand}'");
                    return;
            }
        } else {
            Plugin.Ui.MainWindow.Toggle();
        }
    }
}
