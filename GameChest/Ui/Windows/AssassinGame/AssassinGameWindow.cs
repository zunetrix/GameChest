using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class AssassinGameWindow : Window {
    private Plugin Plugin { get; }
    private string _addPlayerInput = string.Empty;

    public AssassinGameWindow(Plugin plugin) : base("Assassin###AssassinGameWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(460, 420);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var game = Plugin.GameManager.AssassinGame;
        var state = game.State;

        game.Notification.Draw();

        using (ImRaii.Group()) DrawControls(game, state);
        ImGui.Separator();
        ImGui.Spacing();

        using var tabs = ImRaii.TabBar("##AgTabs");
        if (!tabs) return;
        DrawGameTab(game, state);
        DrawHistoryTab(game);
    }

    private void DrawControls(AssassinGame game, AssassinGameState state) {
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (state.Phase is AssassinPhase.Idle or AssassinPhase.Done) {
                if (ImGui.Button("Begin Registration##AgBeginReg")) game.BeginRegistration();
            } else if (state.Phase == AssassinPhase.Registration) {
                using (ImRaii.Disabled(state.Players.Count < Plugin.Config.AssassinGame.MinPlayers))
                    if (ImGui.Button("Assign Targets##AgAssign")) game.AssignTargets();
                if (state.Players.Count < Plugin.Config.AssassinGame.MinPlayers)
                    ImGuiUtil.ToolTip($"Need at least {Plugin.Config.AssassinGame.MinPlayers} players.");
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##AgStop")) game.Stop();
        }

        ImGui.SameLine();
        DrawPhaseBadge(state);
        ImGuiUtil.HelpMarker("Each player has a secret target. GM triggers attacks. Attacker and defender both roll - higher roll wins.");

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        var btnCount = Plugin.Config.DebugMode ? 3 : 2;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * btnCount + spacing * (btnCount - 1) + marginRight));
        if (Plugin.Config.DebugMode) {
            using (ImRaii.Disabled(state.Phase is not (AssassinPhase.Registration or AssassinPhase.Attacking)))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Dice, "##AgSimRoll", "Simulate Roll"))
                    game.SimulateRoll();
            ImGui.SameLine();
        }
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##AgPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.AssassinGame);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##AgSettings", "Settings"))
            Plugin.Ui.AssassinGameSettingsWindow.Toggle();
    }

    private void DrawGameTab(AssassinGame game, AssassinGameState state) {
        using var tab = ImRaii.TabItem("Game##AgGameTab");
        if (!tab) return;

        ImGui.Spacing();

        if (state.Phase == AssassinPhase.Idle) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("Click 'Begin Registration' to start.");
            return;
        }

        if (state.Phase == AssassinPhase.Registration) {
            RegistrationPanel.Draw("Ag", state.Players, ref _addPlayerInput, Plugin.Config.AssassinGame.MinPlayers, n => game.TryRegister(n), Plugin);
            return;
        }

        if (state.Phase == AssassinPhase.Active) {
            ImGui.Text($"Players alive: {state.Players.Count}");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Assignments table
            if (ImGui.BeginTable("##AgAssignTable", 3,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings)) {
                ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Attack", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale);
                ImGui.TableHeadersRow();

                foreach (var (attacker, target) in state.Assignments) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn(); ImGui.Text(ShortName(attacker));
                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Orange)) ImGui.Text(ShortName(target));
                    ImGui.TableNextColumn();
                    if (ImGui.SmallButton($"Attack##{attacker}"))
                        game.TriggerAttack(attacker);
                }
                ImGui.EndTable();
            }
            return;
        }

        if (state.Phase == AssassinPhase.Attacking) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                ImGui.Text($"Attack in progress!");
            ImGui.Spacing();

            ImGui.Text($"Attacker: {ShortName(state.CurrentAttacker ?? "")}");
            ImGui.SameLine();
            if (state.AttackRoll.HasValue) {
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green)) ImGui.Text($"rolled {state.AttackRoll}");
            } else {
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("waiting...");
            }

            ImGui.Text($"Defender: {ShortName(state.CurrentDefender ?? "")}");
            ImGui.SameLine();
            if (state.DefenseRoll.HasValue) {
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green)) ImGui.Text($"rolled {state.DefenseRoll}");
            } else {
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("waiting...");
            }
            return;
        }

        if (state.Phase == AssassinPhase.Done && state.Winner != null) {
            using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                ImGui.Text($"Winner: {ShortName(state.Winner)}");
        }
    }

    private void DrawHistoryTab(AssassinGame game) {
        using var tab = ImRaii.TabItem("History##AgHistTab");
        if (!tab) return;
        if (game.MatchHistory.Count == 0) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("No history yet.");
            return;
        }
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "##AgClearHist", "Ctrl+Click to clear") && ImGui.GetIO().KeyCtrl)
                game.MatchHistory.Clear();
        }
        ImGui.Spacing();
        using var table = ImRaii.Table("##AgHistTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 55 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Winner", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Players", ImGuiTableColumnFlags.WidthFixed, 55 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();
        foreach (var r in game.MatchHistory) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text(r.PlayedAt.ToString("HH:mm"));
            ImGui.TableNextColumn(); using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor)) ImGui.Text(r.Winner);
            ImGui.TableNextColumn(); ImGui.Text($"{r.PlayerCount}");
        }
    }

    private static void DrawPhaseBadge(AssassinGameState state) {
        var (label, color) = state.Phase switch {
            AssassinPhase.Registration => ("[REG]", Style.Colors.Yellow),
            AssassinPhase.Active => ("[ACTIVE]", Style.Colors.Green),
            AssassinPhase.Attacking => ("[ATTACK]", Style.Colors.Orange),
            AssassinPhase.Done => ("[DONE]", Style.Colors.Gray),
            _ => ("[IDLE]", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color)) ImGui.Text(label);
    }

    private static string ShortName(string s) { var i = s.IndexOf('@'); return i >= 0 ? s[..i] : s; }
}
