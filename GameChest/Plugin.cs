using System;
using System.Globalization;

using Dalamud.Plugin;

using GameChest.Resources;

namespace GameChest;

public class Plugin : IDalamudPlugin, IPluginContext {
    internal static string Name => "Game Chest";

    internal Configuration Config { get; }
    internal PluginUi Ui { get; }
    internal PluginCommandManager PluginCommandManager { get; }
    internal GameManager GameManager { get; }
    internal RollManager RollManager { get; }

    internal RollWatcher RollWatcher { get; }
    internal ChatWatcher ChatWatcher { get; }

    Configuration IPluginContext.Config => Config;
    RollManager? IPluginContext.RollManager => RollManager;

    public Plugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<DalamudApi>();
        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(DalamudApi.PluginInterface);

        GameManager = new GameManager(this);
        Ui = new PluginUi(this);
        PluginCommandManager = new PluginCommandManager(this);
        RollWatcher = new RollWatcher(this);
        ChatWatcher = new ChatWatcher(this);
        RollManager = new RollManager(this);

        OnLanguageChange(DalamudApi.PluginInterface.UiLanguage);
        DalamudApi.PluginInterface.LanguageChanged += OnLanguageChange;

        DalamudApi.ClientState.Login += OnLogin;
        DalamudApi.ClientState.Logout += OnLogout;
        DalamudApi.PluginInterface.UiBuilder.Draw += Ui.Draw;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += Ui.SettingsWindow.Toggle;
        DalamudApi.PluginInterface.UiBuilder.OpenMainUi += Ui.MainWindow.Toggle;
        DalamudApi.Framework.Update += OnFrameworkUpdate;

        if (Config.OpenOnStartup) {
            Ui.MainWindow.IsOpen = true;
        }
    }

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework _) {
        if (!DalamudApi.ClientState.IsLoggedIn) return;
        GameManager.Tick(DateTime.Now);
    }

    private static void OnLanguageChange(string langCode) {
        Language.Culture = new CultureInfo(langCode);
    }

    private void OnLogin() {
        if (Config.OpenOnLogin) {
            Ui.MainWindow.IsOpen = true;
        }
    }

    private void OnLogout(int type, int code) {
        Ui.MainWindow.IsOpen = false;
    }

    public void Dispose() {
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi -= Ui.SettingsWindow.Toggle;
        DalamudApi.PluginInterface.UiBuilder.OpenMainUi -= Ui.MainWindow.Toggle;
        DalamudApi.PluginInterface.UiBuilder.Draw -= Ui.Draw;
        DalamudApi.ClientState.Logout -= OnLogout;
        DalamudApi.ClientState.Login -= OnLogin;
        DalamudApi.PluginInterface.LanguageChanged -= OnLanguageChange;
        DalamudApi.Framework.Update -= OnFrameworkUpdate;

        PluginCommandManager.Dispose();
        RollWatcher.Dispose();
        ChatWatcher.Dispose();
        Ui.Dispose();
    }
}

