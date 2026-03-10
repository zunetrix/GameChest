using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

using GameChest.Util.ImGuiExt;

namespace GameChest;

public class PrizeRollSettingsWindow : Window {
    private Plugin Plugin { get; }

    public PrizeRollSettingsWindow(Plugin plugin)
        : base("Prize Roll - Settings###PrizeRollSettingsWindow") {
        Plugin = plugin;

        Size = ImGuiHelpers.ScaledVector2(340, 310);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
    }

    public override void Draw() {
        var cfg = Plugin.Config.PrizeRoll;

        using (ImGuiGroupPanel.BeginGroupPanel("Settings")) {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Max Roll");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
            var maxRoll = cfg.MaxRoll;
            if (ImGui.InputInt("##PrMaxRoll", ref maxRoll, 1, 10)) {
                cfg.MaxRoll = Math.Clamp(maxRoll, 2, 999);
                Plugin.Config.Save();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Output Channel");
            ImGui.SameLine();
            var outChannel = cfg.OutputChannel;
            if (OutputChannelCombo.Draw("##PrOutputChannel", ref outChannel, 180f * ImGuiHelpers.GlobalScale)) {
                cfg.OutputChannel = outChannel;
                Plugin.Config.Save();
            }

            ImGui.Spacing();

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Sorting Mode");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120f * ImGuiHelpers.GlobalScale);
            var modeNames = new[] { "Highest", "Lowest", "Nearest" };
            var modeIdx = (int)cfg.SortingMode;
            if (ImGui.Combo("##SortingMode", ref modeIdx, modeNames, modeNames.Length)) {
                cfg.SortingMode = (PrizeRollSortingMode)modeIdx;
                Plugin.Config.Save();
            }

            if (cfg.SortingMode == PrizeRollSortingMode.Nearest) {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Nearest to");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
                var nearest = cfg.NearestRoll;
                if (ImGui.InputInt("##NearestRoll", ref nearest, 1, 10)) {
                    cfg.NearestRoll = Math.Clamp(nearest, 1, cfg.MaxRoll);
                    Plugin.Config.Save();
                }
            }

            ImGui.Spacing();

            var rerollAllowed = cfg.RerollAllowed;
            if (ImGui.Checkbox("Allow Reroll##RerollAllowed", ref rerollAllowed)) {
                cfg.RerollAllowed = rerollAllowed;
                Plugin.Config.Save();
            }

            if (cfg.RerollAllowed) {
                ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
                var maxRerolls = cfg.MaxRerollsPerPlayer;
                if (ImGui.InputInt("Max per Player##MaxRerolls", ref maxRerolls, 1, 1)) {
                    cfg.MaxRerollsPerPlayer = Math.Max(1, maxRerolls);
                    Plugin.Config.Save();
                }
            }
        }

        ImGui.Spacing();

        using (ImGuiGroupPanel.BeginGroupPanel("Timer")) {
            var useTimer = cfg.UseTimer;
            if (ImGui.Checkbox("Enable##UseTimer", ref useTimer)) {
                cfg.UseTimer = useTimer;
                Plugin.Config.Save();
            }

            if (cfg.UseTimer) {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(55f * ImGuiHelpers.GlobalScale);
                var mins = cfg.TimerDurationSeconds / 60;
                if (ImGui.InputInt("min##TimerMin", ref mins, 0)) {
                    cfg.TimerDurationSeconds = Math.Clamp(mins, 0, 99) * 60 + cfg.TimerDurationSeconds % 60;
                    Plugin.Config.Save();
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(55f * ImGuiHelpers.GlobalScale);
                var secs = cfg.TimerDurationSeconds % 60;
                if (ImGui.InputInt("sec##TimerSec", ref secs, 0)) {
                    cfg.TimerDurationSeconds = cfg.TimerDurationSeconds / 60 * 60 + Math.Clamp(secs, 0, 59);
                    Plugin.Config.Save();
                }
            }
        }
    }
}
