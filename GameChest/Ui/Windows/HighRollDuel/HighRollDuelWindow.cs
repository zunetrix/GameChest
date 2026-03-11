using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class HighRollDuelWindow : Window {
    private Plugin Plugin { get; }
    private string _addPlayerInput = string.Empty;
    private enum RollSort { Default, Highest, Lowest }
    private RollSort _rollSort = RollSort.Lowest;

    public HighRollDuelWindow(Plugin plugin) : base("High Roll Duel###HighRollDuelWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(420, 380);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var game = Plugin.GameManager.HighRollDuelGame;
        var state = game.State;

        game.Notification.Draw();

        using (ImRaii.Group()) DrawControls(game, state);
        ImGui.Separator();
        ImGui.Spacing();

        using var tabs = ImRaii.TabBar("##HrdTabs");
        if (!tabs) return;
        DrawGameTab(game, state);
        DrawHistoryTab(game);
    }

    private void DrawControls(HighRollDuelGame game, HighRollDuelState state) {
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (state.Phase == HighRollDuelPhase.Idle || state.Phase == HighRollDuelPhase.Done) {
                if (ImGui.Button("Begin Registration##HrdBeginReg")) game.BeginRegistration();
            } else if (state.Phase == HighRollDuelPhase.Registration) {
                using (ImRaii.Disabled(state.Players.Count < Plugin.Config.HighRollDuel.MinPlayers))
                    if (ImGui.Button("Start##HrdStart")) game.StartRolling();
                if (state.Players.Count < Plugin.Config.HighRollDuel.MinPlayers)
                    ImGuiUtil.ToolTip($"Need at least {Plugin.Config.HighRollDuel.MinPlayers} players.");
            } else if (state.Phase == HighRollDuelPhase.Rolling) {
                if (ImGui.Button("Close Round##HrdClose")) game.CloseRound();
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##HrdStop")) game.Stop();
        }

        ImGui.SameLine();
        DrawPhaseBadge(state);
        ImGuiUtil.HelpMarker("""
        All players roll /random each round.
        The lowest roll is eliminated.
        Last player standing wins.
        """);

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        var btnCount = Plugin.Config.DebugMode ? 3 : 2;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * btnCount + spacing * (btnCount - 1) + marginRight));
        if (Plugin.Config.DebugMode) {
            using (ImRaii.Disabled(state.Phase is not (HighRollDuelPhase.Registration or HighRollDuelPhase.Rolling)))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Dice, "##HrdSimRoll", "Simulate Roll"))
                    game.SimulateRoll();
            ImGui.SameLine();
        }
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##HrdPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.HighRollDuel);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##HrdSettings", "Settings"))
            Plugin.Ui.HighRollDuelSettingsWindow.Toggle();
    }

    private void DrawGameTab(HighRollDuelGame game, HighRollDuelState state) {
        using var tab = ImRaii.TabItem("Game##HrdGameTab");
        if (!tab) return;

        if (state.Phase == HighRollDuelPhase.Idle) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text("Click 'Begin Registration' to start.");
            return;
        }

        ImGui.Spacing();

        if (state.Phase == HighRollDuelPhase.Registration) {
            RegistrationPanel.Draw("Hrd", state.Players, ref _addPlayerInput, Plugin.Config.HighRollDuel.MinPlayers, n => game.TryRegister(n), Plugin);
            return;
        }

        if (state.Phase == HighRollDuelPhase.Rolling) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                ImGui.Text($"Round {state.Round} - roll /random {Plugin.Config.HighRollDuel.MaxRoll}");
            ImGui.SameLine();
            DrawRollSortButtons();
            ImGui.Spacing();

            var players = _rollSort switch {
                RollSort.Highest => state.Players
                    .OrderByDescending(p => state.CurrentRoundRolls.TryGetValue(p, out var r) ? r : -1)
                    .ToList(),
                RollSort.Lowest => state.Players
                    .OrderBy(p => state.CurrentRoundRolls.TryGetValue(p, out var r) ? r : int.MaxValue)
                    .ToList(),
                _ => (IEnumerable<string>)state.Players,
            };

            if (ImGui.BeginTable("##HrdRollTable", 3,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings)) {
                ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
                ImGui.TableHeadersRow();

                foreach (var p in players) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn(); ImGui.Text(ShortName(p));
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
            return;
        }

        if (state.Phase == HighRollDuelPhase.Done && state.Winner != null) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                ImGui.Text($"Winner: {ShortName(state.Winner)}");
        }
    }

    private void DrawHistoryTab(HighRollDuelGame game) {
        using var tab = ImRaii.TabItem("History##HrdHistTab");
        if (!tab) return;

        if (game.MatchHistory.Count == 0) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("No history yet.");
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "##HrdClearHist", "Ctrl+Click to clear") && ImGui.GetIO().KeyCtrl)
                game.MatchHistory.Clear();
        }
        ImGui.Spacing();

        using var table = ImRaii.Table("##HrdHistTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
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

    private void DrawRollSortButtons() {
        DrawSortToggle("Default", RollSort.Default);
        ImGui.SameLine();
        DrawSortToggle("Highest", RollSort.Highest);
        ImGui.SameLine();
        DrawSortToggle("Lowest", RollSort.Lowest);
    }

    private void DrawSortToggle(string label, RollSort value) {
        var active = _rollSort == value;
        using (active
            ? ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive,  Style.Components.ButtonBlueActive)
            : ImRaii.PushColor(ImGuiCol.Button, Style.Components.Button)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonHovered)
                .Push(ImGuiCol.ButtonActive,  Style.Components.ButtonActive)) {
            if (ImGui.SmallButton($"{label}##HrdSort{value}"))
                _rollSort = value;
        }
    }

    private static void DrawPhaseBadge(HighRollDuelState state) {
        var (label, color) = state.Phase switch {
            HighRollDuelPhase.Registration => ("[REG]", Style.Colors.Yellow),
            HighRollDuelPhase.Rolling => ("[ROLLING]", Style.Colors.Green),
            HighRollDuelPhase.Done => ("[DONE]", Style.Colors.Gray),
            _ => ("[IDLE]", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color)) ImGui.Text(label);
    }

    private static string ShortName(string s) { var i = s.IndexOf('@'); return i >= 0 ? s[..i] : s; }
}
