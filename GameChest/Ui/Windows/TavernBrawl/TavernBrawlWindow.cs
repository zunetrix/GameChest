using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class TavernBrawlWindow : Window {
    private Plugin Plugin { get; }
    private string _addPlayerInput = string.Empty;

    public TavernBrawlWindow(Plugin plugin) : base("Tavern Brawl###TavernBrawlWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(420, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var game = Plugin.GameManager.TavernBrawlGame;
        var state = game.State;

        game.Notification.Draw();

        using (ImRaii.Group()) DrawControls(game, state);
        ImGui.Separator();
        ImGui.Spacing();

        using var tabs = ImRaii.TabBar("##TbTabs");
        if (!tabs) return;
        DrawGameTab(game, state);
        DrawHistoryTab(game);
    }

    private void DrawControls(TavernBrawlGame game, TavernBrawlState state) {
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (state.Phase is TavernBrawlPhase.Idle or TavernBrawlPhase.Done) {
                if (ImGui.Button("Begin Registration##TbBeginReg")) game.BeginRegistration();
            } else if (state.Phase == TavernBrawlPhase.Registration) {
                using (ImRaii.Disabled(state.Players.Count < Plugin.Config.TavernBrawl.MinPlayers))
                    if (ImGui.Button("Start##TbStart")) game.StartRolling();
            } else if (state.Phase == TavernBrawlPhase.Rolling) {
                if (ImGui.Button("Close Round##TbClose")) game.CloseRound();
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##TbStop")) game.Stop();
        }

        ImGui.SameLine();
        DrawPhaseBadge(state);
        ImGuiUtil.HelpMarker("""
        Each round:
        lowest roll is knocked out
        highest roll eliminates another player
        """);

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        var btnCount = Plugin.Config.DebugMode ? 3 : 2;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * btnCount + spacing * (btnCount - 1) + marginRight));
        if (Plugin.Config.DebugMode) {
            using (ImRaii.Disabled(state.Phase is not (TavernBrawlPhase.Registration or TavernBrawlPhase.Rolling)))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Dice, "##TbSimRoll", "Simulate Roll"))
                    game.SimulateRoll();
            ImGui.SameLine();
        }
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##TbPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.TavernBrawl);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##TbSettings", "Settings"))
            Plugin.Ui.TavernBrawlSettingsWindow.Toggle();
    }

    private void DrawGameTab(TavernBrawlGame game, TavernBrawlState state) {
        using var tab = ImRaii.TabItem("Game##TbGameTab");
        if (!tab) return;

        ImGui.Spacing();

        if (state.Phase == TavernBrawlPhase.Idle) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("Click 'Begin Registration' to start.");
            return;
        }

        if (state.Phase == TavernBrawlPhase.Registration) {
            RegistrationPanel.Draw("Tb", state.Players, ref _addPlayerInput, Plugin.Config.TavernBrawl.MinPlayers, n => game.TryRegister(n), Plugin);
            return;
        }

        if (state.Phase == TavernBrawlPhase.Rolling) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                ImGui.Text($"Round {state.Round} - roll /random {Plugin.Config.TavernBrawl.MaxRoll}");
            ImGui.Spacing();
            DrawRollTable(state);
            return;
        }

        if (state.Phase == TavernBrawlPhase.PendingChoice) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Orange))
                ImGui.Text($"{PlayerName.Short(state.HighestRoller ?? "")} rolled {state.HighestRoll} - select a player to eliminate:");
            ImGui.Spacing();

            var eligible = state.Players
                .Where(p => !p.Equals(state.HighestRoller, System.StringComparison.OrdinalIgnoreCase)
                         && !p.Equals(state.LowestRoller, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var p in eligible) {
                if (ImGui.Button($"Eliminate {PlayerName.Short(p)}##TbElim_{p}"))
                    game.EliminateByChoice(p);
            }
            return;
        }

        if (state.Phase == TavernBrawlPhase.Done && state.Winner != null) {
            using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                ImGui.Text($"Winner: {PlayerName.Short(state.Winner)}");
        }
    }

    private static void DrawRollTable(TavernBrawlState state) {
        if (!ImGui.BeginTable("##TbRollTable", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings)) return;
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        foreach (var p in state.Players) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.Text(PlayerName.Short(p));
            ImGui.TableNextColumn();
            if (state.CurrentRoundRolls.TryGetValue(p, out var roll)) ImGui.Text($"{roll}");
            else { using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("..."); }
            ImGui.TableNextColumn();
            var rolled = state.CurrentRoundRolls.ContainsKey(p);
            using (ImRaii.PushColor(ImGuiCol.Text, rolled ? Style.Colors.Green : Style.Colors.Gray))
                ImGui.Text(rolled ? "Rolled" : "Waiting");
        }
        ImGui.EndTable();
    }

    private void DrawHistoryTab(TavernBrawlGame game) {
        using var tab = ImRaii.TabItem("History##TbHistTab");
        if (!tab) return;

        if (game.MatchHistory.Count == 0) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("No history yet.");
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "##TbClearHist", "Ctrl+Click to clear") && ImGui.GetIO().KeyCtrl)
                game.MatchHistory.Clear();
        }
        ImGui.Spacing();

        using var table = ImRaii.Table("##TbHistTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
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

    private static void DrawPhaseBadge(TavernBrawlState state) {
        var (label, color) = state.Phase switch {
            TavernBrawlPhase.Registration => ("[REG]", Style.Colors.Yellow),
            TavernBrawlPhase.Rolling => ("[ROLLING]", Style.Colors.Green),
            TavernBrawlPhase.PendingChoice => ("[CHOOSE]", Style.Colors.Orange),
            TavernBrawlPhase.Done => ("[DONE]", Style.Colors.Gray),
            _ => ("[IDLE]", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color)) ImGui.Text(label);
    }

}
