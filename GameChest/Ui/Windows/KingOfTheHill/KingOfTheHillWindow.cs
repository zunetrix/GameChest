using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class KingOfTheHillWindow : Window {
    private Plugin Plugin { get; }
    private string _addPlayerInput = string.Empty;

    public KingOfTheHillWindow(Plugin plugin) : base("King of the Hill###KingOfTheHillWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(420, 380);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var game = Plugin.GameManager.KingOfTheHillGame;
        var state = game.State;

        game.Notification.Draw();

        using (ImRaii.Group()) DrawControls(game, state);
        ImGui.Separator();
        ImGui.Spacing();

        using var tabs = ImRaii.TabBar("##KothTabs");
        if (!tabs) return;
        DrawGameTab(game, state);
        DrawHistoryTab(game);
    }

    private void DrawControls(KingOfTheHillGame game, KingOfTheHillState state) {
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (state.Phase is KingOfTheHillPhase.Idle or KingOfTheHillPhase.Done) {
                if (ImGui.Button("Begin Registration##KothBeginReg")) game.BeginRegistration();
            } else if (state.Phase == KingOfTheHillPhase.Registration) {
                using (ImRaii.Disabled(state.Players.Count < Plugin.Config.KingOfTheHill.MinPlayers))
                    if (ImGui.Button("Start##KothStart")) game.StartRolling();
            } else if (state.Phase == KingOfTheHillPhase.Rolling) {
                if (ImGui.Button("Close Round##KothClose")) game.CloseRound();
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##KothStop")) game.Stop();
        }

        ImGui.SameLine();
        DrawPhaseBadge(state);
        ImGuiUtil.HelpMarker("Highest roll becomes King. Hold the crown for the configured rounds to win.", sameline: true);

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * 2 + spacing + marginRight));
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##KothPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.KingOfTheHill);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##KothSettings", "Settings"))
            Plugin.Ui.KingOfTheHillSettingsWindow.Toggle();
    }

    private void DrawGameTab(KingOfTheHillGame game, KingOfTheHillState state) {
        using var tab = ImRaii.TabItem("Game##KothGameTab");
        if (!tab) return;

        ImGui.Spacing();

        if (state.Phase == KingOfTheHillPhase.Idle) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("Click 'Begin Registration' to start.");
            return;
        }

        if (state.Phase == KingOfTheHillPhase.Registration) {
            ImGui.Text($"Players: {state.Players.Count}");
            ImGui.Spacing();
            ImGui.SetNextItemWidth(180f * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint("##KothAddPlayer", "Player name...", ref _addPlayerInput, 64);
            ImGui.SameLine();
            using (ImRaii.Disabled(string.IsNullOrWhiteSpace(_addPlayerInput)))
                if (ImGui.Button("Add##KothAddBtn")) { game.TryRegister(_addPlayerInput.Trim()); _addPlayerInput = string.Empty; }
            ImGui.Spacing();
            foreach (var p in state.Players) {
                using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor)) ImGui.Text(ShortName(p));
            }
            return;
        }

        if (state.Phase == KingOfTheHillPhase.Rolling) {
            var cfg = Plugin.Config.KingOfTheHill;
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                ImGui.Text($"Round {state.Round} — roll /random {cfg.MaxRoll}");

            if (state.King != null) {
                ImGui.Spacing();
                ImGui.Text("Current King:");
                ImGui.SameLine();
                using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                    ImGui.Text(ShortName(state.King));
                ImGui.SameLine();
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                    ImGui.Text($"({state.KingHoldCount}/{cfg.CrownHoldRounds} rounds)");
            }

            ImGui.Spacing();
            if (!ImGui.BeginTable("##KothRollTable", 3,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings)) return;
            ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
            ImGui.TableHeadersRow();
            foreach (var p in state.Players) {
                var isKing = p.Equals(state.King, System.StringComparison.OrdinalIgnoreCase);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                using (ImRaii.PushColor(ImGuiCol.Text, isKing ? Plugin.Config.HighlightColor : Style.Colors.White))
                    ImGui.Text(ShortName(p) + (isKing ? " [King]" : ""));
                ImGui.TableNextColumn();
                if (state.CurrentRoundRolls.TryGetValue(p, out var roll)) ImGui.Text($"{roll}");
                else { using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("..."); }
                ImGui.TableNextColumn();
                var rolled = state.CurrentRoundRolls.ContainsKey(p);
                using (ImRaii.PushColor(ImGuiCol.Text, rolled ? Style.Colors.Green : Style.Colors.Gray))
                    ImGui.Text(rolled ? "Rolled" : "Waiting");
            }
            ImGui.EndTable();
            return;
        }

        if (state.Phase == KingOfTheHillPhase.Done && state.Winner != null) {
            using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                ImGui.Text($"Winner: {ShortName(state.Winner)}");
        }
    }

    private void DrawHistoryTab(KingOfTheHillGame game) {
        using var tab = ImRaii.TabItem("History##KothHistTab");
        if (!tab) return;
        if (game.MatchHistory.Count == 0) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("No history yet.");
            return;
        }
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "##KothClearHist", "Ctrl+Click to clear") && ImGui.GetIO().KeyCtrl)
                game.MatchHistory.Clear();
        }
        ImGui.Spacing();
        using var table = ImRaii.Table("##KothHistTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 55 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Winner", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Rounds", ImGuiTableColumnFlags.WidthFixed, 55 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();
        foreach (var r in game.MatchHistory) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text(r.PlayedAt.ToString("HH:mm"));
            ImGui.TableNextColumn(); using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor)) ImGui.Text(r.Winner);
            ImGui.TableNextColumn(); ImGui.Text($"{r.Rounds}");
        }
    }

    private static void DrawPhaseBadge(KingOfTheHillState state) {
        var (label, color) = state.Phase switch {
            KingOfTheHillPhase.Registration => ("[REG]", Style.Colors.Yellow),
            KingOfTheHillPhase.Rolling => ("[ROLLING]", Style.Colors.Green),
            KingOfTheHillPhase.Done => ("[DONE]", Style.Colors.Gray),
            _ => ("[IDLE]", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color)) ImGui.Text(label);
    }

    private static string ShortName(string s) { var i = s.IndexOf('@'); return i >= 0 ? s[..i] : s; }
}
