using System;
using System.Collections.Generic;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using GameChest.Extensions;

namespace GameChest;

internal class ChatWatcher : IDisposable {
    private Plugin Plugin { get; }

    public readonly HashSet<XivChatType> AllowedChatTypes = new()
    {
        XivChatType.Say,
        XivChatType.Party,
        // XivChatType.CrossParty,
        XivChatType.FreeCompany,
        // XivChatType.Alliance,
        XivChatType.Ls1,
        XivChatType.Ls2,
        XivChatType.Ls3,
        XivChatType.Ls4,
        XivChatType.Ls5,
        XivChatType.Ls6,
        XivChatType.Ls7,
        XivChatType.Ls8,
        XivChatType.CrossLinkShell1,
        XivChatType.CrossLinkShell2,
        XivChatType.CrossLinkShell3,
        XivChatType.CrossLinkShell4,
        XivChatType.CrossLinkShell5,
        XivChatType.CrossLinkShell6,
        XivChatType.CrossLinkShell7,
        XivChatType.CrossLinkShell8,
    };

    public ChatWatcher(Plugin plugin) {
        Plugin = plugin;
        DalamudApi.ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose() {
        DalamudApi.ChatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled) {
        if (!Plugin.Config.ListenToChatMessages) return;
        if (isHandled) return;

        var senderName = sender.ToString();
        if (!AllowedChatTypes.Contains(type)
        || !Plugin.Config.ListenedChatTypes.Contains(type)
        || (Plugin.Config.IsBlockListActive && Plugin.Config.Blocklist.ContainsPlayer(senderName))
        ) {
            return;
        }

        var messageString = message.ToString();
        if (messageString.Contains(Plugin.Config.FightGame.JoinGamePhrase, StringComparison.InvariantCultureIgnoreCase)) {
            HandleJoinGame(senderName);
        }

        Plugin.GameManager.ProcessChatMessage(senderName, messageString, type);
    }

    private void HandleJoinGame(string senderName) {
        DalamudApi.PluginLog.Debug($"{senderName} try join the game");
        Plugin.GameManager.FightGame.TryRegister(senderName, RegistrationSource.Chat);
    }
}

