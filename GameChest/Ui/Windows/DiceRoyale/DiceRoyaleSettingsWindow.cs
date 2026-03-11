using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Util.ImGuiExt;

namespace GameChest;

public class DiceRoyaleSettingsWindow : Window {
    private Plugin Plugin { get; }

    public DiceRoyaleSettingsWindow(Plugin plugin) : base("Dice Royale - Settings###DiceRoyaleSettingsWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(320, 220);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var cfg = Plugin.Config.DiceRoyale;
        using (ImGuiGroupPanel.BeginGroupPanel("General")) {
            var outChannel = cfg.OutputChannel;
            if (OutputChannelCombo.Draw("##DrOutput", ref outChannel, 180f * ImGuiHelpers.GlobalScale)) {
                cfg.OutputChannel = outChannel;
                Plugin.Config.Save();
            }
            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var maxRoll = cfg.MaxRoll;
            if (ImGui.InputInt("Max Roll##DrMaxRoll", ref maxRoll, 1, 10)) {
                cfg.MaxRoll = Math.Clamp(maxRoll, 2, 9999);
                Plugin.Config.Save();
            }
            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var minPlayers = cfg.MinPlayers;
            if (ImGui.InputInt("Min Players##DrMinPlayers", ref minPlayers, 1, 1)) {
                cfg.MinPlayers = Math.Clamp(minPlayers, 6, 50);
                Plugin.Config.Save();
            }
            var allowChat = cfg.AllowChatElimination;
            if (ImGui.Checkbox("Allow chat elimination##DrAllowChat", ref allowChat)) {
                cfg.AllowChatElimination = allowChat;
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("Allows the selected player to type the name of the person to eliminate in chat");
        }
        ImGui.Spacing();
        using (ImGuiGroupPanel.BeginGroupPanel("Roll Ranges")) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Red)) ImGui.Text("1-20   Eliminated");
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("21-60  Survive");
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green)) ImGui.Text("61-90  Advantage");
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow)) ImGui.Text("91-100 Eliminate another player");
        }
    }
}
