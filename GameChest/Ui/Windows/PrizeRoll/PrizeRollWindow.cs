using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class PrizeRollWindow : Window {
    private Plugin Plugin { get; }

    public PrizeRollWindow(Plugin plugin)
        : base("Prize Roll###PrizeRollWindow") {
        Plugin = plugin;

        Size = ImGuiHelpers.ScaledVector2(440, 420);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
    }

    public override void Draw() {
        var pr = Plugin.GameManager.PrizeRollGame;
        var state = pr.State;

        if (state.IsTimerRunning && DateTime.Now >= state.TimerEndsAt!.Value)
            pr.Stop();

        pr.Notification.Draw();

        using (ImRaii.Group())
            DrawControls(pr, state);

        ImGui.Separator();
        ImGui.Spacing();

        using var tabs = ImRaii.TabBar("##PrizeTabs");
        if (!tabs) return;

        DrawRollsTab(pr, state);
        DrawHistoryTab(pr);
    }

    private void DrawControls(PrizeRollGame pr, PrizeRollState state) {
        var cfg = Plugin.Config.PrizeRoll;
        using (ImRaii.Disabled(state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (ImGui.Button("Start##StartPrize")) {
                pr.Start();
            }
        }

        ImGui.SameLine();

        using (ImRaii.Disabled(!state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##StopPrize"))
                pr.Stop();
        }

        ImGui.SameLine();

        if (ImGui.Button("Reset##ResetPrize") && ImGui.GetIO().KeyCtrl)
            pr.Reset();
        ImGuiUtil.ToolTip("Ctrl + Click to reset (resets phrase sequence as well)");

        ImGui.SameLine();
        if (state.IsActive) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                ImGui.Text("[ACTIVE]");
        } else {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text("[IDLE]");
        }
        ImGuiUtil.HelpMarker("""
            Players compete by rolling for the best result.
            • GM starts the session and announces the goal.
            • Each player rolls /random once to register their roll.
            • Sorted by Highest / Lowest / Nearest to a target.
            • Player matching the goal wins.
            """);

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        var btnCount = Plugin.Config.DebugMode ? 3 : 2;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * btnCount + spacing * (btnCount - 1) + marginRight));
        if (Plugin.Config.DebugMode) {
            using (ImRaii.Disabled(!state.IsActive))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Dice, "##PrSimRoll", "Simulate Roll"))
                    pr.SimulateRoll();
            ImGui.SameLine();
        }
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##PrPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.PrizeRollGame);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##PrSettings", "Settings"))
            Plugin.Ui.PrizeRollSettingsWindow.Toggle();
    }

    private void DrawRollsTab(PrizeRollGame pr, PrizeRollState state) {
        using var tabItem = ImRaii.TabItem("Rolls##PrRollsTab");
        if (!tabItem) return;

        var cfg = Plugin.Config.PrizeRoll;
        var entries = state.Participants.Entries;

        using (ImRaii.PushColor(ImGuiCol.Button, Style.Colors.GrassGreen, cfg.UseTimer)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.Clock, "##PrToggleTimer", "Toggle Timer")) {
                cfg.UseTimer = !cfg.UseTimer;
                Plugin.Config.Save();
            }
        }

        // Inline sorting controls
        ImGui.SameLine();
        ImGui.Text("Sort:");
        ImGui.SameLine();
        var sortMode = (int)cfg.SortingMode;
        if (ImGui.RadioButton("Highest##SortH", ref sortMode, (int)PrizeRollSortingMode.Highest)) {
            cfg.SortingMode = PrizeRollSortingMode.Highest;
            Plugin.Config.Save();
            pr.Resort();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Lowest##SortL", ref sortMode, (int)PrizeRollSortingMode.Lowest)) {
            cfg.SortingMode = PrizeRollSortingMode.Lowest;
            Plugin.Config.Save();
            pr.Resort();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Nearest##SortN", ref sortMode, (int)PrizeRollSortingMode.Nearest)) {
            cfg.SortingMode = PrizeRollSortingMode.Nearest;
            Plugin.Config.Save();
            pr.Resort();
        }
        if (cfg.SortingMode == PrizeRollSortingMode.Nearest) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(55f * ImGuiHelpers.GlobalScale);
            var nearest = cfg.NearestRoll;
            if (ImGui.InputInt("##NearestVal", ref nearest, 0, 0)) {
                cfg.NearestRoll = Math.Clamp(nearest, 1, 999);
                Plugin.Config.Save();
                pr.Resort();
            }
        }

        if (cfg.UseTimer) {
            ImGui.Spacing();
            DrawTimerRow(state, cfg);
        }

        ImGui.Spacing();

        var hasRolls = pr.RollLog.Count > 0;
        var listH = hasRolls ? ImGui.GetContentRegionAvail().Y * 0.5f : -1f;

        ImGui.BeginChild("##PrParticipants", new Vector2(-1, listH), false);
        if (entries.Count == 0 && !state.IsActive) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text("No active roll. Click \"Start\" to begin.");
        } else if (entries.Count == 0) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text("Waiting for rolls...");
        } else {
            ImGui.Text($"Participants ({entries.Count}):");
            ImGui.Spacing();

            if (ImGui.BeginTable("##PrizeParticipants", 3,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX |
                ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV)) {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
                ImGui.TableHeadersRow();

                for (var i = 0; i < entries.Count; i++) {
                    var p = entries[i];
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"{i + 1}");
                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor, i == 0))
                        ImGui.Text(PlayerName.Short(p.FullName));
                    ImGui.TableNextColumn();
                    ImGui.Text($"{p.RollResult}");
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();

        if (hasRolls) {
            ImGui.Separator();
            ImGui.BeginChild("##PrRollLogArea", new Vector2(-1, -1), false);
            RollLogTable.Draw("##PrRollLog", pr.RollLog, Plugin.Config.HighlightColor);
            ImGui.EndChild();
        }
    }

    private void DrawHistoryTab(PrizeRollGame pr) {
        using var tabItem = ImRaii.TabItem("History##PrHistoryTab");
        if (!tabItem) return;

        if (pr.MatchHistory.Count == 0) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text("No match history yet.");
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
        .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
        .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "##PrClearHistory", "Ctrl+Click to clear history") && ImGui.GetIO().KeyCtrl)
                pr.MatchHistory.Clear();
        }
        ImGui.Spacing();

        using var table = ImRaii.Table("##PrHistoryTable", 6,
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV |
            ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 55f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Winner", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 48f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Mode", ImGuiTableColumnFlags.WidthFixed, 68f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Players", ImGuiTableColumnFlags.WidthFixed, 55f * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        for (var i = 0; i < pr.MatchHistory.Count; i++) {
            var r = pr.MatchHistory[i];
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text($"{i + 1}");
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text(r.PlayedAt.ToString("HH:mm"));
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                ImGui.Text(r.Winner);
            ImGui.TableNextColumn();
            ImGui.Text($"{r.WinningRoll}");
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text(SortingModeLabel(r.SortingMode, r.NearestTarget));
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text($"{r.ParticipantCount}");
        }
    }

    private static void DrawTimerRow(PrizeRollState state, Configuration.PrizeRollConfiguration cfg) {
        if (state.IsTimerRunning) {
            var rem = state.TimeRemaining;
            var color = rem.TotalSeconds < 10 ? Style.Colors.Red
                      : rem.TotalSeconds < 30 ? Style.Colors.Orange
                      : Style.Colors.Green;
            using (ImRaii.PushColor(ImGuiCol.Text, color))
                ImGui.Text($"Time remaining: {(int)rem.TotalMinutes:D2}:{rem.Seconds:D2}");
        } else {
            var m = cfg.TimerDurationSeconds / 60;
            var s = cfg.TimerDurationSeconds % 60;
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text(state.IsActive
                    ? $"Timer: {m:D2}:{s:D2}  (not started)"
                    : $"Timer: {m:D2}:{s:D2}");
        }
    }

    private static string SortingModeLabel(PrizeRollSortingMode mode, int nearestTarget = 0) => mode switch {
        PrizeRollSortingMode.Highest => "Highest",
        PrizeRollSortingMode.Lowest => "Lowest",
        PrizeRollSortingMode.Nearest => $"Nearest {nearestTarget}",
        _ => "?",
    };

}
