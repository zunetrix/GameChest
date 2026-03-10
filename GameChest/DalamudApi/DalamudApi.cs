using System;

using Dalamud.Interface.ImGuiNotification;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace GameChest;

public class DalamudApi {
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null;
    [PluginService] public static IPluginLog PluginLog { get; private set; } = null;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null;
    [PluginService] public static IClientState ClientState { get; private set; } = null;
    // using CSPlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;
    [PluginService] public static IPlayerState PlayerState { get; private set; } = null;
    [PluginService] public static INotificationManager NotificationManager { get; private set; } = null;
    // [PluginService] public static IFramework Framework { get; private set; } = null;
    [PluginService] public static IDataManager DataManager { get; private set; } = null;
    // [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null;
    // [PluginService] public static IObjectTable ObjectTable { get; private set; }
    [PluginService] public static ITargetManager TargetManager { get; private set; }
    // hook
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null;

    private const string PluginPrefixName = $"[FC] ";

    public static void ShowNotification(string message, NotificationType type = NotificationType.None, uint msDelay = 3_000u) => NotificationManager.AddNotification(new Notification { Type = type, Title = PluginPrefixName, Content = message, InitialDuration = TimeSpan.FromMilliseconds(msDelay) });
}
