using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Util.ImGuiExt;

namespace GameChest;

public class DiceBlackjackWindow : Window {
    private Plugin Plugin { get; }
    private string _addPlayerInput = string.Empty;

    public DiceBlackjackWindow(Plugin plugin) : base("Dice Blackjack###DiceBlackjackWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(480, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var game = Plugin.GameManager.DiceBlackjackGame;
        var state = game.State;

        game.Notification.Draw();

        using (ImRaii.Group()) DrawControls(game, state);
        ImGui.Separator();
        ImGui.Spacing();

        DrawGameContent(game, state);
    }

    private void DrawControls(DiceBlackjackGame game, DiceBlackjackState state) {
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (state.Phase is DiceBlackjackPhase.Idle or DiceBlackjackPhase.Finished) {
                if (ImGui.Button("Begin Registration##DbjBeginReg")) game.BeginRegistration();
            } else if (state.Phase == DiceBlackjackPhase.Registering) {
                var canStart = state.Players.Count >= Plugin.Config.DiceBlackjack.MinPlayers;
                using (ImRaii.Disabled(!canStart))
                    if (ImGui.Button("Start Game##DbjStart")) game.StartGame();
                if (!canStart) ImGuiUtil.ToolTip($"Need at least {Plugin.Config.DiceBlackjack.MinPlayers} player(s).");
            } else if (state.Phase == DiceBlackjackPhase.PlayerTurns) {
                var canStand = state.CurrentPlayer?.DealCount >= 2;
                using (ImRaii.Disabled(!canStand))
                    if (ImGui.Button("Stand##DbjStand")) game.Stand();
                if (!canStand) ImGuiUtil.ToolTip("Player must receive 2 deal cards first.");
            } else if (state.Phase == DiceBlackjackPhase.DealerTurn) {
                var canDraw = state.DealerStatus == PlayerHandStatus.Active;
                using (ImRaii.Disabled(!canDraw))
                    if (ImGui.Button("Draw Dealer Card##DbjDraw"))
                        Chat.SendMessage($"/random {Plugin.Config.DiceBlackjack.MaxRoll}");
                ImGui.SameLine();
            }
        }

        if (state.Phase == DiceBlackjackPhase.DealerTurn) {
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                var canStand = state.DealerStatus == PlayerHandStatus.Active;
                using (ImRaii.Disabled(!canStand))
                    if (ImGui.Button("Dealer Stands##DbjDealerStand")) game.DealerStand();
            }
            ImGui.SameLine();
        }

        if (state.Phase != DiceBlackjackPhase.Idle && state.Phase != DiceBlackjackPhase.Finished)
            ImGui.SameLine();

        using (ImRaii.Disabled(!state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##DbjStop")) game.Stop();
        }

        ImGui.SameLine();
        DrawPhaseBadge(state);
        ImGuiUtil.HelpMarker("""
        Players roll /random {maxroll} to join.
        Each player receives 2 cards, then decides Hit or Stand.
        Reach the target without going over to beat the dealer.
        Players type "stand" in chat, or GM clicks Stand.
        During dealer turn: click "Draw Dealer Card" to send /random to chat.
        The roll is captured automatically. Click "Dealer Stands" to stop early.
        """);

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        var btnCount = Plugin.Config.DebugMode ? 3 : 2;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * btnCount + spacing * (btnCount - 1) + marginRight));
        if (Plugin.Config.DebugMode) {
            using (ImRaii.Disabled(state.Phase is not (DiceBlackjackPhase.Registering or DiceBlackjackPhase.PlayerTurns or DiceBlackjackPhase.DealerTurn)))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Dice, "##DbjSimRoll", "Simulate Roll"))
                    game.SimulateRoll();
            ImGui.SameLine();
        }
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##DbjPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.DiceBlackjack);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##DbjSettings", "Settings"))
            Plugin.Ui.DiceBlackjackSettingsWindow.Toggle();
    }

    private void DrawGameContent(DiceBlackjackGame game, DiceBlackjackState state) {
        switch (state.Phase) {
            case DiceBlackjackPhase.Idle:
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                    ImGui.Text("Click 'Begin Registration' to start.");
                return;

            case DiceBlackjackPhase.Registering:
                RegistrationPanel.Draw("Dbj", state.Players.Select(p => p.Name).ToList(),
                    ref _addPlayerInput, Plugin.Config.DiceBlackjack.MinPlayers, n => game.TryJoin(n, JoinSource.Manual), Plugin);
                return;

            case DiceBlackjackPhase.PlayerTurns:
                DrawPlayerTurns(game, state);
                return;

            case DiceBlackjackPhase.DealerTurn:
                DrawDealerTurn(game, state);
                return;

            case DiceBlackjackPhase.Finished:
                DrawResults(game, state);
                return;
        }
    }

    private void DrawPlayerTurns(DiceBlackjackGame game, DiceBlackjackState state) {
        var current = state.CurrentPlayer;
        var cfg = Plugin.Config.DiceBlackjack;

        if (current != null) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                ImGui.Text($"{PlayerName.Short(current.Name)}'s turn");
            if (current.DealCount < 2) {
                ImGui.SameLine();
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                    ImGui.Text($"— deal cards: {current.DealCount}/2 (roll /random {cfg.MaxRoll})");
            } else {
                ImGui.SameLine();
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                    ImGui.Text($"— total: {game.HandTotal(current.Cards)}  (Hit: roll | Stand: type \"stand\" or click)");
            }
            ImGui.Spacing();
        }

        DrawHandTable(game, state, showCurrentHighlight: true);
    }

    private void DrawDealerTurn(DiceBlackjackGame game, DiceBlackjackState state) {
        var cfg = Plugin.Config.DiceBlackjack;
        var dealerTotal = game.HandTotal(state.DealerCards);
        var dealerHand = string.Join(", ", state.DealerCards.Select(c => game.CardLabel(c)));

        using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Orange))
            ImGui.Text("Dealer's turn");
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) {
            if (state.DealerCards.Count == 0)
                ImGui.Text("— no cards yet");
            else
                ImGui.Text($"— [{dealerHand}]  Total: {dealerTotal}  (Stand at: {cfg.DealerStandAt})");
        }
        ImGui.Spacing();
        DrawHandTable(game, state, showCurrentHighlight: false);
    }

    private void DrawResults(DiceBlackjackGame game, DiceBlackjackState state) {
        var dealerTotal = game.HandTotal(state.DealerCards);
        var dealerBusted = state.DealerStatus == PlayerHandStatus.Busted;
        var dealerHand = string.Join(", ", state.DealerCards.Select(c => game.CardLabel(c)));

        using (ImRaii.PushColor(ImGuiCol.Text, dealerBusted ? Style.Colors.Red : Style.Colors.Gray))
            ImGui.Text($"Dealer: [{dealerHand}] = {dealerTotal}{(dealerBusted ? " (BUST)" : "")}");

        if (state.Winner != null) {
            ImGui.SameLine(0, 20f * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                ImGui.Text($"Champion: {PlayerName.Short(state.Winner)}");
        }

        ImGui.Spacing();
        DrawHandTable(game, state, showCurrentHighlight: false);
    }

    private void DrawHandTable(DiceBlackjackGame game, DiceBlackjackState state, bool showCurrentHighlight) {
        using var table = ImRaii.Table("##DbjHandTable", 4,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Player",  ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Cards",   ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Total",   ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Status",  ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        for (var i = 0; i < state.Players.Count; i++) {
            var p = state.Players[i];
            var isCurrent = showCurrentHighlight && i == state.CurrentPlayerIndex
                            && state.Phase == DiceBlackjackPhase.PlayerTurns;
            var total = game.HandTotal(p.Cards);
            var cards = p.DealCount > 0
                ? string.Join(", ", p.Cards.Select(c => game.CardLabel(c)))
                : "—";

            var (statusLabel, statusColor) = p.Status switch {
                PlayerHandStatus.Standing => ("Stand", Style.Colors.Blue),
                PlayerHandStatus.Busted   => ("Bust",  Style.Colors.Red),
                _                         => (p.DealCount < 2 ? "Dealing" : "Active", Style.Colors.Green),
            };

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, isCurrent ? Style.Colors.Yellow : Vector4.One))
                ImGui.Text(isCurrent ? $"\uE05D {PlayerName.Short(p.Name)}" : PlayerName.Short(p.Name));
            ImGui.TableNextColumn();
            ImGui.Text(cards);
            ImGui.TableNextColumn();
            if (p.DealCount > 0) ImGui.Text(total.ToString());
            else { using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray)) ImGui.Text("—"); }
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, statusColor))
                ImGui.Text(statusLabel);
        }
    }

    private static void DrawPhaseBadge(DiceBlackjackState state) {
        var (label, color) = state.Phase switch {
            DiceBlackjackPhase.Registering => ("[REG]",     Style.Colors.Yellow),
            DiceBlackjackPhase.PlayerTurns  => ("[PLAYING]", Style.Colors.Green),
            DiceBlackjackPhase.DealerTurn   => ("[DEALER]",  Style.Colors.Orange),
            DiceBlackjackPhase.Finished         => ("[DONE]",    Style.Colors.Gray),
            _                               => ("[IDLE]",    Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color)) ImGui.Text(label);
    }

}
