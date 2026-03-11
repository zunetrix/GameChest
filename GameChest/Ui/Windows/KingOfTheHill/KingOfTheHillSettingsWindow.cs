using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Util.ImGuiExt;

namespace GameChest;

public class KingOfTheHillSettingsWindow : Window {
    private Plugin Plugin { get; }

    public KingOfTheHillSettingsWindow(Plugin plugin) : base("King of the Hill - Settings###KingOfTheHillSettingsWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(320, 220);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var cfg = Plugin.Config.KingOfTheHill;
        using (ImGuiGroupPanel.BeginGroupPanel("General")) {
            var outChannel = cfg.OutputChannel;
            if (OutputChannelCombo.Draw("##KothOutput", ref outChannel, 180f * ImGuiHelpers.GlobalScale)) {
                cfg.OutputChannel = outChannel;
                Plugin.Config.Save();
            }
            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var maxRoll = cfg.MaxRoll;
            if (ImGui.InputInt("Max Roll##KothMaxRoll", ref maxRoll, 1, 10)) {
                cfg.MaxRoll = Math.Clamp(maxRoll, 2, 9999);
                Plugin.Config.Save();
            }
            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var minPlayers = cfg.MinPlayers;
            if (ImGui.InputInt("Min Players##KothMinPlayers", ref minPlayers, 1, 1)) {
                cfg.MinPlayers = Math.Clamp(minPlayers, 3, 50);
                Plugin.Config.Save();
            }
            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var holdRounds = cfg.CrownHoldRounds;
            if (ImGui.InputInt("Crown Hold Rounds##KothHold", ref holdRounds, 1, 1)) {
                cfg.CrownHoldRounds = Math.Clamp(holdRounds, 1, 20);
                Plugin.Config.Save();
            }
            ImGuiUtil.HelpMarker("Rounds the king must hold the crown to win.", sameline: true);
        }
    }
}
