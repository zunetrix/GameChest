using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Util.ImGuiExt;

namespace GameChest;

public class TavernBrawlSettingsWindow : Window {
    private Plugin Plugin { get; }

    public TavernBrawlSettingsWindow(Plugin plugin) : base("Tavern Brawl - Settings###TavernBrawlSettingsWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(320, 200);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var cfg = Plugin.Config.TavernBrawl;
        using (ImGuiGroupPanel.BeginGroupPanel("General")) {
            var outChannel = cfg.OutputChannel;
            if (OutputChannelCombo.Draw("##TbOutput", ref outChannel, 180f * ImGuiHelpers.GlobalScale)) {
                cfg.OutputChannel = outChannel;
                Plugin.Config.Save();
            }
            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var maxRoll = cfg.MaxRoll;
            if (ImGui.InputInt("Max Roll##TbMaxRoll", ref maxRoll, 1, 10)) {
                cfg.MaxRoll = Math.Clamp(maxRoll, 2, 9999);
                Plugin.Config.Save();
            }
            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var minPlayers = cfg.MinPlayers;
            if (ImGui.InputInt("Min Players##TbMinPlayers", ref minPlayers, 1, 1)) {
                cfg.MinPlayers = Math.Clamp(minPlayers, 4, 50);
                Plugin.Config.Save();
            }
            var allowChat = cfg.AllowChatElimination;
            if (ImGui.Checkbox("Allow chat elimination##TbAllowChat", ref allowChat)) {
                cfg.AllowChatElimination = allowChat;
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("Allows the selected player to type the name of the person to eliminate in chat");
        }
    }
}
