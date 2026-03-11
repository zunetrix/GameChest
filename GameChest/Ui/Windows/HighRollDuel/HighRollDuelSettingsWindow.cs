using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Util.ImGuiExt;

namespace GameChest;

public class HighRollDuelSettingsWindow : Window {
    private Plugin Plugin { get; }

    public HighRollDuelSettingsWindow(Plugin plugin) : base("High Roll Duel - Settings###HighRollDuelSettingsWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(320, 200);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var cfg = Plugin.Config.HighRollDuel;

        using (ImGuiGroupPanel.BeginGroupPanel("General")) {
            var outChannel = cfg.OutputChannel;
            if (OutputChannelCombo.Draw("##HrdOutput", ref outChannel, 180f * ImGuiHelpers.GlobalScale)) {
                cfg.OutputChannel = outChannel;
                Plugin.Config.Save();
            }

            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var maxRoll = cfg.MaxRoll;
            if (ImGui.InputInt("Max Roll##HrdMaxRoll", ref maxRoll, 1, 10)) {
                cfg.MaxRoll = Math.Clamp(maxRoll, 2, 9999);
                Plugin.Config.Save();
            }

            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var minPlayers = cfg.MinPlayers;
            if (ImGui.InputInt("Min Players##HrdMinPlayers", ref minPlayers, 1, 1)) {
                cfg.MinPlayers = Math.Clamp(minPlayers, 2, 50);
                Plugin.Config.Save();
            }

            var autoClose = cfg.AutoCloseRound;
            if (ImGui.Checkbox("Auto close round##HrdAutoClose", ref autoClose)) {
                cfg.AutoCloseRound = autoClose;
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("Automatically close the round when all players have rolled.\nDisable to control timing manually with the Close Round button.");
        }
    }
}
