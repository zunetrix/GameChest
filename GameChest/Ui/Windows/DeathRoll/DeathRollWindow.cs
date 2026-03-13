using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class DeathRollWindow : Window {
    private Plugin Plugin { get; }

    public DeathRollWindow(Plugin plugin)
        : base("Death Roll###DeathRollWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(440, 380);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
    }

    public override void Draw() {
        var dr = Plugin.GameManager.DeathRollGame;
        var state = dr.State;

        dr.Notification.Draw();

        using (ImRaii.Group())
            DrawControls(dr, state);

        ImGui.Separator();
        ImGui.Spacing();

        using var tabs = ImRaii.TabBar("##DrTabs");
        if (!tabs) return;

        DrawRollTab(dr, state);
        DrawHistoryTab(dr);
    }

    private void DrawControls(DeathRollGame dr, DeathRollState state) {
        // Start
        using (ImRaii.Disabled(state.Phase == DeathRollPhase.Active))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (ImGui.Button("Start##DrStart"))
                dr.Start();
        }

        ImGui.SameLine();

        // Stop
        using (ImRaii.Disabled(state.Phase != DeathRollPhase.Active))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##DrStop"))
                dr.Stop();
        }

        ImGui.SameLine();

        // New Round
        using (ImRaii.Disabled(state.Phase == DeathRollPhase.Idle))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
            if (ImGui.Button("New Round##DrNewRound"))
                dr.RestartMatch();
        }

        ImGui.SameLine();

        if (ImGui.Button("Reset##DrReset") && ImGui.GetIO().KeyCtrl)
            dr.Reset();
        ImGuiUtil.ToolTip("Ctrl + Click to reset");

        // Status badge
        ImGui.SameLine();
        var (label, color) = state.Phase switch {
            DeathRollPhase.Active => ("[ACTIVE]", Style.Colors.Green),
            DeathRollPhase.Finished => ("[DONE]", Style.Colors.Yellow),
            _ => ("[IDLE]", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.Text(label);
        ImGuiUtil.HelpMarker("""
            Two players trade rolls in a decreasing chain.
            • First player rolls /random {max} to start.
            • Next player must roll /random {last_result}.
            • Players must alternate - can't roll twice in a row.
            • Whoever rolls 1 loses the round.
            """);

        // Right-aligned icon buttons
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        var btnCount = Plugin.Config.DebugMode ? 3 : 2;
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * btnCount + spacing * (btnCount - 1) + marginRight));
        if (Plugin.Config.DebugMode) {
            using (ImRaii.Disabled(!state.IsActive))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Dice, "##DrSimRoll", "Simulate Roll"))
                    dr.SimulateRoll();
            ImGui.SameLine();
        }
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##DrPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.DeathRoll);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##DrSettings", "Settings"))
            Plugin.Ui.DeathRollSettingsWindow.Toggle();
    }

    private void DrawRollTab(DeathRollGame dr, DeathRollState state) {
        using var tabItem = ImRaii.TabItem("Roll##DrRollTab");
        if (!tabItem) return;

        ImGui.Spacing();

        // Winner / Loser when Finished
        if (state.Phase == DeathRollPhase.Finished) {
            if (state.Winner != null) {
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                    ImGui.Text($"\uE05D WINNER: {state.Winner}");
            }
            if (state.Loser != null) {
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Red))
                    ImGui.Text($"\uE05D LOSER:  {state.Loser}");
            }
            ImGui.Spacing();
        }

        // Chain table
        if (state.Chain.Count == 0) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text("No rolls yet. Start a round and let the chain begin.");
            return;
        }

        using var table = ImRaii.Table("##DrChain", 4,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV |
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.PadOuterX);
        if (!table) return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("OutOf", ImGuiTableColumnFlags.WidthFixed, 54 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        for (var i = 0; i < state.Chain.Count; i++) {
            var entry = state.Chain[i];
            var isLast = i == state.Chain.Count - 1;

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text($"{i + 1:00}");

            ImGui.TableNextColumn();
            var isLoser = state.Phase == DeathRollPhase.Finished && isLast;
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Red, isLoser))
                ImGui.Text(PlayerName.Short(entry.PlayerName));

            ImGui.TableNextColumn();
            var rollColor = entry.Result == 1 ? Style.Colors.Red : Plugin.Config.HighlightColor;
            using (ImRaii.PushColor(ImGuiCol.Text, rollColor))
                ImGui.Text(entry.Result.ToString());

            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text(entry.OutOf.ToString());
        }
    }

    private static void DrawHistoryTab(DeathRollGame dr) {
        using var tabItem = ImRaii.TabItem("History##DrHistory");
        if (!tabItem) return;

        ImGui.Spacing();

        if (dr.MatchHistory.Count == 0) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text("No completed rounds yet.");
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "##DrClearHistory", "Ctrl+Click to clear history") && ImGui.GetIO().KeyCtrl)
                dr.MatchHistory.Clear();
        }
        ImGui.Spacing();

        using var table = ImRaii.Table("##DrHistoryTable", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX);
        if (!table) return;

        ImGui.TableSetupColumn("Winner", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Loser", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        foreach (var r in dr.MatchHistory) {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                ImGui.Text(r.Winner);
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Red))
                ImGui.Text(r.Loser);
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text(r.PlayedAt.ToString("HH:mm"));
        }
    }

}
