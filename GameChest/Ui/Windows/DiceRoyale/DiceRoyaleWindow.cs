using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class DiceRoyaleWindow : Window {
    private Plugin Plugin { get; }
    private string _addPlayerInput = string.Empty;

    public DiceRoyaleWindow(Plugin plugin) : base("Dice Royale###DiceRoyaleWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(440, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var game = Plugin.GameManager.DiceRoyaleGame;
        var state = game.State;

        game.Notification.Draw();

        using (ImRaii.Group()) DrawControls(game, state);
        ImGui.Separator();
        ImGui.Spacing();

        using var tabs = ImRaii.TabBar("##DrTabs");
        if (!tabs) return;
        DrawGameTab(game, state);
        DrawHistoryTab(game);
    }

    private void DrawControls(DiceRoyaleGame game, DiceRoyaleState state) {
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (state.Phase is DiceRoyalePhase.Idle or DiceRoyalePhase.Done) {
                if (ImGui.Button("Begin Registration##DrBeginReg")) game.BeginRegistration();
            } else if (state.Phase == DiceRoyalePhase.Registration) {
                using (ImRaii.Disabled(state.Players.Count < Plugin.Config.DiceRoyale.MinPlayers))
                    if (ImGui.Button("Start##DrStart")) game.StartRolling();
            } else if (state.Phase == DiceRoyalePhase.Rolling) {
                if (ImGui.Button("Close Round##DrClose")) game.CloseRound();
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##DrStop")) game.Stop();
        }

        ImGui.SameLine();
        DrawPhaseBadge(state);
        ImGuiUtil.HelpMarker("""
        Roll ranges determine fate each round:
        1-20 eliminated,
        61-90 advantage,
        91-100 eliminate another player
        """);

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        var btnCount = Plugin.Config.DebugMode ? 3 : 2;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * btnCount + spacing * (btnCount - 1) + marginRight));
        if (Plugin.Config.DebugMode) {
            using (ImRaii.Disabled(state.Phase is not (DiceRoyalePhase.Registration or DiceRoyalePhase.Rolling)))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Dice, "##DrSimRoll", "Simulate Roll"))
                    game.SimulateRoll();
            ImGui.SameLine();
        }
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##DrPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.DiceRoyale);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##DrSettings", "Settings"))
            Plugin.Ui.DiceRoyaleSettingsWindow.Toggle();
    }

    private void DrawGameTab(DiceRoyaleGame game, DiceRoyaleState state) {
        using var tab = ImRaii.TabItem("Game##DrGameTab");
        if (!tab) return;

        ImGui.Spacing();

        if (state.Phase == DiceRoyalePhase.Idle) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("Click 'Begin Registration' to start.");
            return;
        }

        if (state.Phase == DiceRoyalePhase.Registration) {
            RegistrationPanel.Draw("Dr", state.Players, ref _addPlayerInput, Plugin.Config.DiceRoyale.MinPlayers, n => game.TryJoin(n, JoinSource.Manual), Plugin);
            return;
        }

        if (state.Phase == DiceRoyalePhase.Rolling) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                ImGui.Text($"Round {state.Round} - roll /random {Plugin.Config.DiceRoyale.MaxRoll}");
            ImGui.Spacing();
            DrawRollTable(state);
            return;
        }

        if (state.Phase == DiceRoyalePhase.PendingElimination) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Orange))
                ImGui.Text($"{PlayerName.Short(state.CurrentEliminator ?? "")} must eliminate a player:");
            ImGui.Spacing();

            var eligible = state.Players
                .Where(p => !p.Equals(state.CurrentEliminator, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var p in eligible) {
                if (ImGui.Button($"Eliminate {PlayerName.Short(p)}##DrElim_{p}"))
                    game.EliminateByChoice(p);
            }
            return;
        }

        if (state.Phase == DiceRoyalePhase.Done && state.Winner != null) {
            using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                ImGui.Text($"Winner: {PlayerName.Short(state.Winner)}");
        }
    }

    private static void DrawRollTable(DiceRoyaleState state) {
        if (!ImGui.BeginTable("##DrRollTable", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings)) return;
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();
        foreach (var p in state.Players) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.Text(PlayerName.Short(p));
            ImGui.TableNextColumn();
            if (state.CurrentRoundRolls.TryGetValue(p, out var roll)) {
                var color = roll <= 20 ? Style.Colors.Red : roll <= 60 ? Style.Colors.Gray : roll <= 90 ? Style.Colors.Green : Style.Colors.Yellow;
                using (ImRaii.PushColor(ImGuiCol.Text, color)) ImGui.Text($"{roll}");
            } else { using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("..."); }
            ImGui.TableNextColumn();
            var rolled = state.CurrentRoundRolls.ContainsKey(p);
            using (ImRaii.PushColor(ImGuiCol.Text, rolled ? Style.Colors.Green : Style.Colors.Gray))
                ImGui.Text(rolled ? "Rolled" : "Waiting");
        }
        ImGui.EndTable();
    }

    private void DrawHistoryTab(DiceRoyaleGame game) {
        using var tab = ImRaii.TabItem("History##DrHistTab");
        if (!tab) return;
        if (game.MatchHistory.Count == 0) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("No history yet.");
            return;
        }
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "##DrClearHist", "Ctrl+Click to clear") && ImGui.GetIO().KeyCtrl)
                game.MatchHistory.Clear();
        }
        ImGui.Spacing();
        using var table = ImRaii.Table("##DrHistTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 55 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Winner", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Rounds", ImGuiTableColumnFlags.WidthFixed, 55 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();
        foreach (var r in game.MatchHistory) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text(r.PlayedAt.ToString("HH:mm"));
            ImGui.TableNextColumn(); using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor)) ImGui.Text(r.Winner);
            ImGui.TableNextColumn(); ImGui.Text($"{r.PlayerCount}");
        }
    }

    private static void DrawPhaseBadge(DiceRoyaleState state) {
        var (label, color) = state.Phase switch {
            DiceRoyalePhase.Registration => ("[REG]", Style.Colors.Yellow),
            DiceRoyalePhase.Rolling => ("[ROLLING]", Style.Colors.Green),
            DiceRoyalePhase.PendingElimination => ("[CHOOSE]", Style.Colors.Orange),
            DiceRoyalePhase.Done => ("[DONE]", Style.Colors.Gray),
            _ => ("[IDLE]", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color)) ImGui.Text(label);
    }

}
