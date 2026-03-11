using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class DeathRollTournamentWindow : Window {
    private Plugin Plugin { get; }

    private string _manualPlayerName = string.Empty;

    public DeathRollTournamentWindow(Plugin plugin)
        : base("DeathRoll Tournament###DeathRollTournamentWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(460, 480);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
    }

    public override void Draw() {
        var tn = Plugin.GameManager.DeathRollTournamentGame;
        var state = tn.State;

        tn.Notification.Draw();

        using (ImRaii.Group())
            DrawControls(tn, state);

        ImGui.Separator();
        ImGui.Spacing();

        using var tabs = ImRaii.TabBar("##TnTabs");
        if (!tabs) return;

        DrawTournamentTab(tn, state);
        DrawHistoryTab(tn);
    }

    private void DrawControls(DeathRollTournamentGame tn, DeathRollTournamentState state) {
        // Phase-based action button
        if (state.Phase is DeathRollTournamentPhase.Idle or DeathRollTournamentPhase.Done) {
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
                if (ImGui.Button("Begin Registration##TnBeginReg"))
                    tn.BeginRegistration();
            }
            ImGui.SameLine();
        } else if (state.Phase == DeathRollTournamentPhase.Registration) {
            using (ImRaii.Disabled(state.RegisteredPlayers.Count < 2))
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Close Registration##TnCloseReg"))
                    tn.CloseRegistration();
            }
            if (state.RegisteredPlayers.Count < 2)
                ImGuiUtil.ToolTip("Need at least 2 players.");
            ImGui.SameLine();
        } else if (state.Phase == DeathRollTournamentPhase.Preparing) {
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
                if (ImGui.Button("Start Tournament##TnStart"))
                    tn.StartMatch();
            }
            ImGui.SameLine();
        }

        // Stop / Reset
        using (ImRaii.Disabled(!state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##TnStop"))
                tn.Stop();
        }

        ImGui.SameLine();

        if (ImGui.Button("Reset##TnReset") && ImGui.GetIO().KeyCtrl)
            tn.Reset();
        ImGuiUtil.ToolTip("Ctrl + Click to reset");

        ImGui.SameLine();

        // Status badge
        var (label, color) = state.Phase switch {
            DeathRollTournamentPhase.Registration => ("[REGISTRATION]", Style.Colors.Yellow),
            DeathRollTournamentPhase.Preparing => ("[PREPARING]", Style.Colors.Orange),
            DeathRollTournamentPhase.Match => ("[MATCH]", Style.Colors.Green),
            DeathRollTournamentPhase.Done => ("[DONE]", Style.Colors.Blue),
            _ => ("[IDLE]", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.Text(label);
        ImGuiUtil.HelpMarker("""
            Bracket tournament using Death Roll duels.
            • Players roll /random (no number) to register.
            • GM closes registration to generate the bracket.
            • Each match is a 1v1 Death Roll - whoever rolls 1 loses.
            • Winner advances; last player standing wins the tournament.
            """);

        // Right-aligned icon buttons
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        var btnCount = Plugin.Config.DebugMode ? 3 : 2;
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * btnCount + spacing * (btnCount - 1) + marginRight));
        if (Plugin.Config.DebugMode) {
            using (ImRaii.Disabled(state.Phase != DeathRollTournamentPhase.Match))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Dice, "##TnSimRoll", "Simulate Roll"))
                    tn.SimulateRoll();
            ImGui.SameLine();
        }
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##TnPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.DeathRollTournament);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##TnSettings", "Settings"))
            Plugin.Ui.DeathRollTournamentSettingsWindow.Toggle();
    }

    private void DrawTournamentTab(DeathRollTournamentGame tn, DeathRollTournamentState state) {
        using var tabItem = ImRaii.TabItem("Tournament##TnTab");
        if (!tabItem) return;

        ImGui.Spacing();

        switch (state.Phase) {
            case DeathRollTournamentPhase.Idle:
                DrawIdleSection();
                break;
            case DeathRollTournamentPhase.Registration:
                DrawRegistrationSection(tn, state);
                break;
            case DeathRollTournamentPhase.Preparing:
                DrawPreparingSection(tn, state);
                break;
            case DeathRollTournamentPhase.Match:
                DrawMatchSection(tn, state);
                break;
            case DeathRollTournamentPhase.Done:
                DrawDoneSection(state);
                break;
        }
    }

    private static void DrawIdleSection() {
        using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
            ImGui.TextWrapped("Click \"Begin Registration\" above to start accepting participants via /random.");
    }

    private void DrawRegistrationSection(DeathRollTournamentGame tn, DeathRollTournamentState state) {
        ImGui.Text($"Participants: {state.RegisteredPlayers.Count}");
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
            ImGui.Text("(players roll /random to join)");

        ImGui.Spacing();

        // Player list table
        if (state.RegisteredPlayers.Count > 0) {
            using (var table = ImRaii.Table("##TnRegTable", 3,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV |
                ImGuiTableFlags.ScrollY | ImGuiTableFlags.PadOuterX,
                new Vector2(-1, 120 * ImGuiHelpers.GlobalScale))) {
                if (table) {
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
                    ImGui.TableHeadersRow();

                    string? toRemove = null;
                    for (var i = 0; i < state.RegisteredPlayers.Count; i++) {
                        var player = state.RegisteredPlayers[i];
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text($"{i + 1:00}");
                        ImGui.TableNextColumn();
                        ImGui.Text(player);
                        ImGui.TableNextColumn();
                        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
                            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
                            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
                            if (ImGuiUtil.IconButton(FontAwesomeIcon.Trash, $"##TnDel{i}", "Remove (Ctrl+Click)") &&
                                ImGui.GetIO().KeyCtrl)
                                toRemove = player;
                        }
                    }
                    if (toRemove != null) tn.RemovePlayer(toRemove);
                }
            }
        }

        ImGui.Spacing();

        // Manual add
        ImGui.SetNextItemWidth(280f * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##TnManualAdd", "Firstname Lastname[@World]", ref _manualPlayerName, 100);
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (ImGui.Button("Add##TnManualReg") && !string.IsNullOrWhiteSpace(_manualPlayerName)) {
                tn.TryRegisterPlayer(_manualPlayerName.Trim());
                _manualPlayerName = string.Empty;
            }
        }
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Crosshairs, "##TnTargetReg", "Register targeted player")) {
            var target = DalamudApi.TargetManager.Target;
            if (target != null)
                tn.TryRegisterPlayer(target.Name.TextValue);
        }
    }

    private static void DrawPreparingSection(DeathRollTournamentGame tn, DeathRollTournamentState state) {
        ImGui.Text($"Players: {state.RegisteredPlayers.Count(p => p != DeathRollTournamentMatch.PlaceholderPlayer)} - Bracket generated");
        ImGui.Spacing();

        // Bracket preview
        for (var ri = 0; ri < state.Rounds.Count; ri++) {
            var round = state.Rounds[ri];
            var header = $"Round {round.RoundNumber} ({round.Matches.Count} match{(round.Matches.Count != 1 ? "es" : "")})##TnRound{ri}";
            if (ImGui.CollapsingHeader(header)) {
                ImGui.Indent();
                foreach (var m in round.Matches) {
                    var winStr = m.Winner != null ? $"  → {ShortName(m.Winner)}" : "";
                    ImGui.Text($"{ShortName(m.Player1)} vs {ShortName(m.Player2)}{winStr}");
                }
                ImGui.Unindent();
            }
        }

        ImGui.Spacing();
    }

    private void DrawMatchSection(DeathRollTournamentGame tn, DeathRollTournamentState state) {
        var round = state.CurrentRound;
        if (round == null || state.MatchPlayer1 == null || state.MatchPlayer2 == null) return;

        var matchNum = state.CurrentMatchIndex + 1;
        var totalMatches = round.Matches.Count;
        ImGui.Text($"Round {round.RoundNumber} - Match {matchNum}/{totalMatches}:");
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
            ImGui.Text($"{ShortName(state.MatchPlayer1)} vs {ShortName(state.MatchPlayer2)}");

        ImGui.Spacing();

        // Chain table
        if (state.MatchChain.Count == 0) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text($"Waiting for first roll - /random {(state.MatchStartingRoll == 999 ? "" : state.MatchStartingRoll.ToString())}");
        } else {
            DrawChainTable(state);
        }

        // Winner panel or forfeit buttons
        ImGui.Spacing();
        if (state.MatchWinner != null) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                ImGui.Text($"\uE05D Match Winner: {ShortName(state.MatchWinner)}");
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
                if (ImGui.Button("Next Match##TnNext"))
                    tn.AdvanceToNextMatch();
            }
        } else {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text("Forfeit:");
            ImGui.SameLine();
            if (ImGui.Button($"→ {ShortName(state.MatchPlayer1)}##TnForf1"))
                tn.ForfeitToPlayer(state.MatchPlayer1);
            ImGui.SameLine();
            if (ImGui.Button($"→ {ShortName(state.MatchPlayer2)}##TnForf2"))
                tn.ForfeitToPlayer(state.MatchPlayer2);
        }
    }

    private void DrawChainTable(DeathRollTournamentState state) {
        using var table = ImRaii.Table("##TnChain", 4,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV |
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.PadOuterX,
            new Vector2(-1, 120 * ImGuiHelpers.GlobalScale));
        if (!table) return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("OutOf", ImGuiTableColumnFlags.WidthFixed, 54 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        for (var i = 0; i < state.MatchChain.Count; i++) {
            var entry = state.MatchChain[i];
            var isLast = i == state.MatchChain.Count - 1;

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text($"{i + 1:00}");
            ImGui.TableNextColumn();
            var isLoser = state.MatchWinner != null && isLast;
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Red, isLoser))
                ImGui.Text(ShortName(entry.PlayerName));
            ImGui.TableNextColumn();
            var rollColor = entry.Result == 1 ? Style.Colors.Red : Plugin.Config.HighlightColor;
            using (ImRaii.PushColor(ImGuiCol.Text, rollColor))
                ImGui.Text(entry.Result.ToString());
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text(entry.OutOf.ToString());
        }
    }

    private static void DrawDoneSection(DeathRollTournamentState state) {
        ImGui.Spacing();
        ImGui.Spacing();

        if (state.TournamentWinner != null) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow)) {
                var text = $"\uE05D Champion: {state.TournamentWinner} \uE05D";
                var textSize = ImGui.CalcTextSize(text);
                ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - textSize.X) * 0.5f);
                ImGui.Text(text);
            }
        }
    }

    private static void DrawHistoryTab(DeathRollTournamentGame tn) {
        using var tabItem = ImRaii.TabItem("History##TnHistory");
        if (!tabItem) return;

        ImGui.Spacing();

        if (tn.MatchHistory.Count == 0) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text("No completed tournaments yet.");
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "##TnClearHistory", "Ctrl+Click to clear history") && ImGui.GetIO().KeyCtrl)
                tn.MatchHistory.Clear();
        }
        ImGui.Spacing();

        using var table = ImRaii.Table("##TnHistoryTable", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX);
        if (!table) return;

        ImGui.TableSetupColumn("Champion", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Players", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        foreach (var r in tn.MatchHistory) {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                ImGui.Text(r.Winner);
            ImGui.TableNextColumn();
            ImGui.Text(r.PlayerCount.ToString());
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text(r.PlayedAt.ToString("HH:mm"));
        }
    }

    private static string ShortName(string fullName) {
        if (fullName == DeathRollTournamentMatch.PlaceholderPlayer) return DeathRollTournamentMatch.PlaceholderPlayer;
        var at = fullName.IndexOf('@');
        return at >= 0 ? fullName[..at] : fullName;
    }
}
