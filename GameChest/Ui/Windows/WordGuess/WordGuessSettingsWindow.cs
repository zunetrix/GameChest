using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

using GameChest.Util.ImGuiExt;

namespace GameChest;

public class WordGuessSettingsWindow : Window {
    private Plugin Plugin { get; }

    public WordGuessSettingsWindow(Plugin plugin)
        : base("Word Guess - Settings###WordGuessSettingsWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(360, 390);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var cfg = Plugin.Config.WordGuess;

        // ── Match ─────────────────────────────────────────────
        using (ImGuiGroupPanel.BeginGroupPanel("Match")) {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Output Channel");
            ImGui.SameLine();
            var outChannel = cfg.OutputChannel;
            if (OutputChannelCombo.Draw("##WgOutputChannel", ref outChannel, 180f * ImGuiHelpers.GlobalScale)) {
                cfg.OutputChannel = outChannel;
                Plugin.Config.Save();
            }

            ImGui.Spacing();

            var modeNames = new[] { "Single (each round independent)", "Session (most correct wins)" };
            var modeIdx = (int)cfg.VictoryMode;
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Victory Mode");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(210f * ImGuiHelpers.GlobalScale);
            if (ImGui.Combo("##WgVictoryMode", ref modeIdx, modeNames, modeNames.Length)) {
                cfg.VictoryMode = (WordGuessVictoryMode)modeIdx;
                Plugin.Config.Save();
            }

            ImGui.Spacing();

            var partial = cfg.AllowPartialMatch;
            if (ImGui.Checkbox("Allow Partial Match##WgPartial", ref partial)) {
                cfg.AllowPartialMatch = partial;
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("Accept answer if it is contained anywhere in the player's message.");

            var caseSens = cfg.CaseSensitive;
            if (ImGui.Checkbox("Case Sensitive##WgCaseSensitive", ref caseSens)) {
                cfg.CaseSensitive = caseSens;
                Plugin.Config.Save();
            }

            var autoAdv = cfg.AutoAdvance;
            if (ImGui.Checkbox("Auto Advance##WgAutoAdvance", ref autoAdv)) {
                cfg.AutoAdvance = autoAdv;
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("Automatically start the next question after a round ends (won or timed out).");
        }

        ImGui.Spacing();

        // ── Timer ─────────────────────────────────────────────
        using (ImGuiGroupPanel.BeginGroupPanel("Timer")) {
            var useTimer = cfg.UseGlobalTimer;
            if (ImGui.Checkbox("Global Timer##WgUseTimer", ref useTimer)) {
                cfg.UseGlobalTimer = useTimer;
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("Apply a countdown to every question. Individual questions can override with their own timer.");

            if (cfg.UseGlobalTimer) {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(55f * ImGuiHelpers.GlobalScale);
                var mins = cfg.GlobalTimerSecs / 60;
                if (ImGui.InputInt("min##WgTimerMin", ref mins, 0)) {
                    cfg.GlobalTimerSecs = Math.Clamp(mins, 0, 99) * 60 + cfg.GlobalTimerSecs % 60;
                    Plugin.Config.Save();
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(55f * ImGuiHelpers.GlobalScale);
                var secs = cfg.GlobalTimerSecs % 60;
                if (ImGui.InputInt("sec##WgTimerSec", ref secs, 0)) {
                    cfg.GlobalTimerSecs = cfg.GlobalTimerSecs / 60 * 60 + Math.Clamp(secs, 0, 59);
                    Plugin.Config.Save();
                }
            }
        }

        ImGui.Spacing();

        // ── Hint ──────────────────────────────────────────────
        using (ImGuiGroupPanel.BeginGroupPanel("Hint")) {
            var revealHint = cfg.RevealHint;
            if (ImGui.Checkbox("Auto Reveal Hint##WgRevealHint", ref revealHint)) {
                cfg.RevealHint = revealHint;
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("Automatically publish the question's hint after a delay. Only applies to questions that have a hint configured.");

            if (cfg.RevealHint) {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(70f * ImGuiHelpers.GlobalScale);
                var afterSecs = cfg.RevealHintAfterSecs;
                if (ImGui.InputInt("sec after start##WgHintSecs", ref afterSecs, 5)) {
                    cfg.RevealHintAfterSecs = Math.Max(1, afterSecs);
                    Plugin.Config.Save();
                }
            }
        }
    }
}
