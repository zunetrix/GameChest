using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Util.ImGuiExt;

namespace GameChest;

public class AssassinGameSettingsWindow : Window {
    private Plugin Plugin { get; }

    public AssassinGameSettingsWindow(Plugin plugin) : base("Assassin Game - Settings###AssassinGameSettingsWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(320, 200);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var cfg = Plugin.Config.AssassinGame;
        using (ImGuiGroupPanel.BeginGroupPanel("General")) {
            var outChannel = cfg.OutputChannel;
            if (OutputChannelCombo.Draw("##AgOutput", ref outChannel, 180f * ImGuiHelpers.GlobalScale)) {
                cfg.OutputChannel = outChannel;
                Plugin.Config.Save();
            }
            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var maxRoll = cfg.MaxRoll;
            if (ImGui.InputInt("Max Roll##AgMaxRoll", ref maxRoll, 1, 10)) {
                cfg.MaxRoll = Math.Clamp(maxRoll, 2, 9999);
                Plugin.Config.Save();
            }
            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var minPlayers = cfg.MinPlayers;
            if (ImGui.InputInt("Min Players##AgMinPlayers", ref minPlayers, 1, 1)) {
                cfg.MinPlayers = Math.Clamp(minPlayers, 5, 50);
                Plugin.Config.Save();
            }
        }
    }
}
