using System;
using System.Collections.Generic;

using Dalamud.Game.Chat;
using Dalamud.Game.Text;

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

    private void OnChatMessage(IHandleableChatMessage message) {
        if (!Plugin.Config.ListenToChatMessages) return;
        if (message.IsHandled) return;

        var senderName = SanitizeSenderName(message.Sender.ToString());
        if (!AllowedChatTypes.Contains(message.LogKind)
        || !Plugin.Config.ListenedChatTypes.Contains(message.LogKind)
        || (Plugin.Config.IsBlockListActive && Plugin.Config.Blocklist.ContainsPlayer(senderName))
        ) {
            // DalamudApi.PluginLog.Warning($"{senderName} is on the blocklist and will be ignored");
            return;
        }

        var messageString = message.Message.ToString();

        Plugin.GameManager.ProcessChatMessage(senderName, messageString, message.LogKind);
    }

    private static string SanitizeSenderName(string raw) {
        var i = 0;
        while (i < raw.Length && !char.IsLetter(raw[i])) i++;
        return i > 0 ? raw[i..] : raw;
    }
}

