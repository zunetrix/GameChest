using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

using GameChest.Util.ImGuiExt;

namespace GameChest;

public class DeathRollSettingsWindow : Window {
    private Plugin Plugin { get; }

    public DeathRollSettingsWindow(Plugin plugin)
        : base("Death Roll - Settings###DeathRollSettingsWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(300, 160);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
    }

    public override void Draw() {
        var cfg = Plugin.Config.DeathRoll;

        using (ImGuiGroupPanel.BeginGroupPanel("Settings")) {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Starting Roll");
            ImGuiUtil.ToolTip("The OutOf value for the first roll in the chain.\n999 = /random without a number.");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
            var startingRoll = cfg.StartingRoll;
            if (ImGui.InputInt("##DrStartingRoll", ref startingRoll, 1, 10)) {
                cfg.StartingRoll = Math.Clamp(startingRoll, 2, 999);
                Plugin.Config.Save();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Output Channel");
            ImGui.SameLine();
            var outChannel = cfg.OutputChannel;
            if (OutputChannelCombo.Draw("##DrOutputChannel", ref outChannel, 180f * ImGuiHelpers.GlobalScale)) {
                cfg.OutputChannel = outChannel;
                Plugin.Config.Save();
            }
        }
    }
}
