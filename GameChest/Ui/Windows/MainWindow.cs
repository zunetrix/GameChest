using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Resources;
using GameChest.Util;
using GameChest.Util.ImGuiExt;

namespace GameChest;

public class MainWindow : Window {
    private Plugin Plugin { get; }
    private PluginUi Ui { get; }
    private static readonly Version Version = typeof(MainWindow).Assembly.GetName().Version;

    internal MainWindow(Plugin plugin, PluginUi ui) : base($"{Plugin.Name} {Version}###FcMainWindow") {
        Plugin = plugin;
        Ui = ui;

        Size = ImGuiHelpers.ScaledVector2(380, 300);
        SizeCondition = ImGuiCond.FirstUseEver;
        UpdateWindowConfig();
    }

    public override void PreDraw() {
        Flags = ImGuiWindowFlags.None;
        if (!Plugin.Config.AllowMovement) Flags |= ImGuiWindowFlags.NoMove;
        if (!Plugin.Config.AllowResize) Flags |= ImGuiWindowFlags.NoResize;

        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
        base.PreDraw();
    }

    public override bool DrawConditions() => true;

    public override void Draw() {
        using (ImRaii.Group())
            DrawHeader();

        DrawGameCards();
    }

    private void DrawHeader() {
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##GamePhrasesBtn", Language.GamePhrases))
            Ui.GamePhrasesWindow.Toggle();

        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Ban, "##BlocklistBtn", Language.BlockList))
            Ui.BlocklistWindow.Toggle();

        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##SettingsBtn"))
            Ui.SettingsWindow.Toggle();
        ImGuiUtil.ToolTip(Language.SettingsTitle);

        ImGui.SameLine();
        DrawVisibleGamesButton();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    private void DrawVisibleGamesButton() {
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Eye, "##VisibleGamesBtn", "Visible Games"))
            ImGui.OpenPopup("VisibleGamesPopup");

        using var borderColor = ImRaii.PushColor(ImGuiCol.Border, Style.Components.TooltipBorderColor);
        using var borderSize = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1);
        using var popup = ImRaii.Popup("VisibleGamesPopup");
        if (!popup) return;

        ImGui.TextDisabled("Visible Games");
        ImGui.Separator();
        ImGui.Spacing();

        var c = Plugin.Config.MainWindowCards;
        var changed = false;
        if (ImGui.Checkbox("Prize Roll", ref c.PrizeRoll)) changed = true;
        if (ImGui.Checkbox("Fight Club", ref c.FightGame)) changed = true;
        if (ImGui.Checkbox("Death Roll", ref c.DeathRoll)) changed = true;
        if (ImGui.Checkbox("DeathRoll Tournament", ref c.DeathRollTournament)) changed = true;
        if (ImGui.Checkbox("Word Guess", ref c.WordGuess)) changed = true;
        if (ImGui.Checkbox("High Roll Duel", ref c.HighRollDuel)) changed = true;
        if (ImGui.Checkbox("Tavern Brawl", ref c.TavernBrawl)) changed = true;
        if (ImGui.Checkbox("Dice Royale", ref c.DiceRoyale)) changed = true;
        if (ImGui.Checkbox("King of the Hill", ref c.KingOfTheHill)) changed = true;
        if (ImGui.Checkbox("Assassin Game", ref c.AssassinGame)) changed = true;
        if (ImGui.Checkbox("Dice Blackjack", ref c.DiceBlackjack)) changed = true;
        if (changed) Plugin.Config.Save();
    }

    private void DrawGameCards() {
        var v = Plugin.Config.MainWindowCards;
        var first = true;

        void Card(bool visible, Action draw) {
            if (!visible) return;
            if (!first) ImGui.Spacing();
            draw();
            first = false;
        }

        Card(v.PrizeRoll, DrawPrizeRollCard);
        Card(v.FightGame, DrawFightGameCard);
        Card(v.DeathRoll, DrawDeathRollCard);
        Card(v.DeathRollTournament, DrawTournamentCard);
        Card(v.WordGuess, DrawWordGuessCard);
        Card(v.HighRollDuel, DrawHighRollDuelCard);
        Card(v.TavernBrawl, DrawTavernBrawlCard);
        Card(v.DiceRoyale, DrawDiceRoyaleCard);
        Card(v.KingOfTheHill, DrawKingOfTheHillCard);
        Card(v.AssassinGame, DrawAssassinGameCard);
        Card(v.DiceBlackjack, DrawDiceBlackjackCard);
    }

    private void DrawFightGameCard() {
        var state = Plugin.GameManager.FightGame.State;

        using (ImGuiGroupPanel.BeginGroupPanel("Fight Club")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenFightGame", new Vector2(btnW, 0)))
                    Ui.FightGameWindow.Toggle();
            }

            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            DrawFightStatus(state);
        }
    }

    private void DrawFightStatus(FightState state) {
        var (label, color) = state.Phase switch {
            FightPhase.Registering => ("REGISTRATION", Style.Colors.Yellow),
            FightPhase.Initiative => ("INITIATIVE", Style.Colors.Orange),
            FightPhase.Combat => ("COMBAT", Style.Colors.Green),
            FightPhase.Finished => ("DONE", Style.Colors.Gray),
            _ => ("IDLE", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.Text($"[{label}]");

        switch (state.Phase) {
            case FightPhase.Combat when state.CurrentAttacker != null && state.PlayerA != null && state.PlayerB != null:
                using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                    ImGui.Text($"  Turn ({state.TurnNumber}): {PlayerName.Short(state.CurrentAttacker.FullName)}");
                using (var table = ImRaii.Table("##MHPTable", 2, ImGuiTableFlags.None)) {
                    if (table) {
                        ImGui.TableSetupColumn("##MHPName", ImGuiTableColumnFlags.WidthFixed, 120f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("##MHPBar", ImGuiTableColumnFlags.WidthStretch);
                        DrawMiniHpRow(state.PlayerA, state.CurrentAttacker.FullName == state.PlayerA.FullName);
                        DrawMiniHpRow(state.PlayerB, state.CurrentAttacker.FullName == state.PlayerB.FullName);
                    }
                }
                break;

            case FightPhase.Registering:
                ImGui.Text($"  Fighters: {state.RegisteredFighters.Count}/2");
                break;

            case FightPhase.Initiative:
                ImGui.Text("  Rolling for initiative...");
                break;

            default:
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                    ImGui.Text("  No active fight.");
                break;
        }
    }

    private void DrawMiniHpRow(FighterState fighter, bool isCurrentTurn) {
        var frac = fighter.MaxHealth > 0 ? (float)fighter.Health / fighter.MaxHealth : 0f;
        var col = frac > 0.5f ? Style.Colors.GrassGreen : frac > 0.2f ? Style.Colors.Orange : Style.Colors.Red;

        ImGui.TableNextColumn();
        var name = PlayerName.Short(fighter.FullName);
        if (isCurrentTurn) {
            using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                ImGui.Text($"\uE05D {name}");
        } else {
            ImGui.Text($"  {name}");
        }

        ImGui.TableNextColumn();
        using (ImRaii.PushColor(ImGuiCol.PlotHistogram, col))
            ImGui.ProgressBar(frac, new Vector2(-1, 0), $"{fighter.Health}/{fighter.MaxHealth}");
    }

    private void DrawPrizeRollCard() {
        var pr = Plugin.GameManager.PrizeRollGame;
        var state = pr.State;

        using (ImGuiGroupPanel.BeginGroupPanel("Prize Roll")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenPrizeRoll", new Vector2(btnW, 0)))
                    Ui.PrizeRollWindow.Toggle();
            }

            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            if (state.IsActive) {
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                    ImGui.Text("[ACTIVE]");
                var count = state.Participants.Entries.Count;
                ImGui.Text($"  {count} participant{(count != 1 ? "s" : "")}");
                var first = state.Participants.First;
                if (first != null) {
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  Leading: \uE05D {PlayerName.Short(first.FullName)} ({first.RollResult})");
                }
            } else {
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                    ImGui.Text("[IDLE]");
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                    ImGui.Text("  No active session.");
            }
        }
    }

    private void DrawDeathRollCard() {
        var dr = Plugin.GameManager.DeathRollGame;
        var state = dr.State;

        using (ImGuiGroupPanel.BeginGroupPanel("Death Roll")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenDeathRoll", new Vector2(btnW, 0)))
                    Ui.DeathRollWindow.Toggle();
            }

            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            switch (state.Phase) {
                case DeathRollPhase.Active:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                        ImGui.Text("[ACTIVE]");
                    ImGui.Text($"  Chain: {state.Chain.Count} roll{(state.Chain.Count != 1 ? "s" : "")}");
                    break;
                case DeathRollPhase.Finished:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                        ImGui.Text("[DONE]");
                    if (state.Winner != null)
                        ImGui.Text($"  Winner: {PlayerName.Short(state.Winner)}");
                    break;
                default:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                        ImGui.Text("[IDLE]");
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                        ImGui.Text("  No active round.");
                    break;
            }
        }
    }

    private void DrawTournamentCard() {
        var tn = Plugin.GameManager.DeathRollTournamentGame;
        var state = tn.State;

        using (ImGuiGroupPanel.BeginGroupPanel("DeathRoll Tournament")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenTournament", new Vector2(btnW, 0)))
                    Ui.DeathRollTournamentWindow.Toggle();
            }

            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            var (cardLabel, cardCol) = state.Phase switch {
                DeathRollTournamentPhase.Registering => ("[REGISTRATION]", Style.Colors.Yellow),
                DeathRollTournamentPhase.Preparing => ("[PREPARING]", Style.Colors.Orange),
                DeathRollTournamentPhase.Match => ("[MATCH]", Style.Colors.Green),
                DeathRollTournamentPhase.Finished => ("[DONE]", Style.Colors.Blue),
                _ => ("[IDLE]", Style.Colors.Gray),
            };
            using (ImRaii.PushColor(ImGuiCol.Text, cardCol))
                ImGui.Text(cardLabel);

            switch (state.Phase) {
                case DeathRollTournamentPhase.Registering:
                    ImGui.Text($"  {state.RegisteredPlayers.Count} registered");
                    break;
                case DeathRollTournamentPhase.Match when state.MatchPlayer1 != null && state.MatchPlayer2 != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  {PlayerName.Short(state.MatchPlayer1)} vs {PlayerName.Short(state.MatchPlayer2)}");
                    break;
                case DeathRollTournamentPhase.Finished when state.TournamentWinner != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                        ImGui.Text($"  Champion: {PlayerName.Short(state.TournamentWinner)}");
                    break;
                default:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                        ImGui.Text("  No active tournament.");
                    break;
            }
        }
    }

    private void DrawWordGuessCard() {
        var wg = Plugin.GameManager.WordGuessGame;
        var state = wg.State;
        var cfg = Plugin.Config.WordGuess;

        using (ImGuiGroupPanel.BeginGroupPanel("Word Guess")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenWordGuess", new Vector2(btnW, 0)))
                    Ui.WordGuessWindow.Toggle();
            }

            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            var (cardLabel, cardCol) = state.Phase switch {
                WordGuessPhase.Active => ("[ACTIVE]", Style.Colors.Green),
                WordGuessPhase.Finished => ("[DONE]", Style.Colors.Blue),
                _ => ("[IDLE]", Style.Colors.Gray),
            };
            using (ImRaii.PushColor(ImGuiCol.Text, cardCol))
                ImGui.Text(cardLabel);

            switch (state.Phase) {
                case WordGuessPhase.Active when wg.CurrentQuestion != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                        ImGui.Text($"  Q {state.CurrentQuestionIndex + 1}/{cfg.Questions.Count}");
                    break;
                case WordGuessPhase.Finished:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Blue))
                        ImGui.Text($"  Session complete - {state.SessionRounds.Count} rounds");
                    break;
                default:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                        ImGui.Text($"  {cfg.Questions.Count} question(s) ready");
                    break;
            }
        }
    }

    private void DrawHighRollDuelCard() {
        var state = Plugin.GameManager.HighRollDuelGame.State;
        using (ImGuiGroupPanel.BeginGroupPanel("High Roll Duel")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenHrd", new Vector2(btnW, 0)))
                    Ui.HighRollDuelWindow.Toggle();
            }
            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            var (label, col) = state.Phase switch {
                HighRollDuelPhase.Registering => ("[REG]", Style.Colors.Yellow),
                HighRollDuelPhase.Rolling => ("[ROLLING]", Style.Colors.Green),
                HighRollDuelPhase.Finished => ("[DONE]", Style.Colors.Gray),
                _ => ("[IDLE]", Style.Colors.Gray),
            };
            using (ImRaii.PushColor(ImGuiCol.Text, col)) ImGui.Text(label);
            switch (state.Phase) {
                case HighRollDuelPhase.Registering:
                    ImGui.Text($"  {state.Players.Count} registered");
                    break;
                case HighRollDuelPhase.Rolling:
                    ImGui.Text($"  Round {state.Round} - {state.CurrentRoundRolls.Count}/{state.Players.Count} rolled");
                    break;
                case HighRollDuelPhase.Finished when state.Winner != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  Winner: {PlayerName.Short(state.Winner)}");
                    break;
                default:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                        ImGui.Text("  No active game.");
                    break;
            }
        }
    }

    private void DrawTavernBrawlCard() {
        var state = Plugin.GameManager.TavernBrawlGame.State;
        using (ImGuiGroupPanel.BeginGroupPanel("Tavern Brawl")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenTb", new Vector2(btnW, 0)))
                    Ui.TavernBrawlWindow.Toggle();
            }
            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            var (label, col) = state.Phase switch {
                TavernBrawlPhase.Registering => ("[REG]", Style.Colors.Yellow),
                TavernBrawlPhase.Rolling => ("[ROLLING]", Style.Colors.Green),
                TavernBrawlPhase.PendingChoice => ("[CHOICE]", Style.Colors.Orange),
                TavernBrawlPhase.Finished => ("[DONE]", Style.Colors.Gray),
                _ => ("[IDLE]", Style.Colors.Gray),
            };
            using (ImRaii.PushColor(ImGuiCol.Text, col)) ImGui.Text(label);
            switch (state.Phase) {
                case TavernBrawlPhase.Registering:
                    ImGui.Text($"  {state.Players.Count} registered");
                    break;
                case TavernBrawlPhase.Rolling:
                    ImGui.Text($"  Round {state.Round} - {state.CurrentRoundRolls.Count}/{state.Players.Count} rolled");
                    break;
                case TavernBrawlPhase.PendingChoice when state.HighestRoller != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  {PlayerName.Short(state.HighestRoller)} chooses");
                    break;
                case TavernBrawlPhase.Finished when state.Winner != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  Winner: {PlayerName.Short(state.Winner)}");
                    break;
                default:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                        ImGui.Text("  No active brawl.");
                    break;
            }
        }
    }

    private void DrawDiceRoyaleCard() {
        var state = Plugin.GameManager.DiceRoyaleGame.State;
        using (ImGuiGroupPanel.BeginGroupPanel("Dice Royale")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenDr", new Vector2(btnW, 0)))
                    Ui.DiceRoyaleWindow.Toggle();
            }
            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            var (label, col) = state.Phase switch {
                DiceRoyalePhase.Registering => ("[REG]", Style.Colors.Yellow),
                DiceRoyalePhase.Rolling => ("[ROLLING]", Style.Colors.Green),
                DiceRoyalePhase.PendingElimination => ("[ELIM]", Style.Colors.Orange),
                DiceRoyalePhase.Finished => ("[DONE]", Style.Colors.Gray),
                _ => ("[IDLE]", Style.Colors.Gray),
            };
            using (ImRaii.PushColor(ImGuiCol.Text, col)) ImGui.Text(label);
            switch (state.Phase) {
                case DiceRoyalePhase.Registering:
                    ImGui.Text($"  {state.Players.Count} registered");
                    break;
                case DiceRoyalePhase.Rolling:
                    ImGui.Text($"  Round {state.Round} - {state.Players.Count} remaining");
                    break;
                case DiceRoyalePhase.PendingElimination when state.CurrentEliminator != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  {PlayerName.Short(state.CurrentEliminator)} eliminates");
                    break;
                case DiceRoyalePhase.Finished when state.Winner != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  Winner: {PlayerName.Short(state.Winner)}");
                    break;
                default:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                        ImGui.Text("  No active royale.");
                    break;
            }
        }
    }

    private void DrawKingOfTheHillCard() {
        var state = Plugin.GameManager.KingOfTheHillGame.State;
        using (ImGuiGroupPanel.BeginGroupPanel("King of the Hill")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenKoth", new Vector2(btnW, 0)))
                    Ui.KingOfTheHillWindow.Toggle();
            }
            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            var (label, col) = state.Phase switch {
                KingOfTheHillPhase.Registering => ("[REG]", Style.Colors.Yellow),
                KingOfTheHillPhase.Rolling => ("[ROLLING]", Style.Colors.Green),
                KingOfTheHillPhase.Finished => ("[DONE]", Style.Colors.Gray),
                _ => ("[IDLE]", Style.Colors.Gray),
            };
            using (ImRaii.PushColor(ImGuiCol.Text, col)) ImGui.Text(label);
            switch (state.Phase) {
                case KingOfTheHillPhase.Registering:
                    ImGui.Text($"  {state.Players.Count} registered");
                    break;
                case KingOfTheHillPhase.Rolling when state.King != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                        ImGui.Text($"  King: {PlayerName.Short(state.King)} ({state.KingHoldCount}/{Plugin.Config.KingOfTheHill.CrownHoldRounds})");
                    break;
                case KingOfTheHillPhase.Finished when state.Winner != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  Champion: {PlayerName.Short(state.Winner)}");
                    break;
                default:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                        ImGui.Text("  No active game.");
                    break;
            }
        }
    }

    private void DrawAssassinGameCard() {
        var state = Plugin.GameManager.AssassinGame.State;
        using (ImGuiGroupPanel.BeginGroupPanel("Assassin Game")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenAss", new Vector2(btnW, 0)))
                    Ui.AssassinGameWindow.Toggle();
            }
            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            var (label, col) = state.Phase switch {
                AssassinPhase.Registering => ("[REG]", Style.Colors.Yellow),
                AssassinPhase.Active => ("[ACTIVE]", Style.Colors.Green),
                AssassinPhase.Attacking => ("[ATTACK]", Style.Colors.Orange),
                AssassinPhase.Finished => ("[DONE]", Style.Colors.Gray),
                _ => ("[IDLE]", Style.Colors.Gray),
            };
            using (ImRaii.PushColor(ImGuiCol.Text, col)) ImGui.Text(label);
            switch (state.Phase) {
                case AssassinPhase.Registering:
                    ImGui.Text($"  {state.Players.Count} registered");
                    break;
                case AssassinPhase.Active:
                    ImGui.Text($"  {state.Players.Count} players alive");
                    break;
                case AssassinPhase.Attacking when state.CurrentAttacker != null && state.CurrentDefender != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  {PlayerName.Short(state.CurrentAttacker)} vs {PlayerName.Short(state.CurrentDefender)}");
                    break;
                case AssassinPhase.Finished when state.Winner != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  Winner: {PlayerName.Short(state.Winner)}");
                    break;
                default:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                        ImGui.Text("  No active game.");
                    break;
            }
        }
    }

    private void DrawDiceBlackjackCard() {
        var state = Plugin.GameManager.DiceBlackjackGame.State;
        using (ImGuiGroupPanel.BeginGroupPanel("Dice Blackjack")) {
            var btnW = 60f * ImGuiHelpers.GlobalScale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnW);
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
                if (ImGui.Button("Open##OpenDbj", new Vector2(btnW, 0)))
                    Ui.DiceBlackjackWindow.Toggle();
            }
            ImGui.SameLine(8f * ImGuiHelpers.GlobalScale);
            var (label, col) = state.Phase switch {
                DiceBlackjackPhase.Registering => ("[REG]", Style.Colors.Yellow),
                DiceBlackjackPhase.PlayerTurns => ("[PLAYING]", Style.Colors.Green),
                DiceBlackjackPhase.DealerTurn => ("[DEALER]", Style.Colors.Orange),
                DiceBlackjackPhase.Finished => ("[DONE]", Style.Colors.Gray),
                _ => ("[IDLE]", Style.Colors.Gray),
            };
            using (ImRaii.PushColor(ImGuiCol.Text, col)) ImGui.Text(label);
            switch (state.Phase) {
                case DiceBlackjackPhase.Registering:
                    ImGui.Text($"  {state.Players.Count} registered");
                    break;
                case DiceBlackjackPhase.PlayerTurns when state.CurrentPlayer != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                        ImGui.Text($"  {PlayerName.Short(state.CurrentPlayer.Name)}'s turn ({state.CurrentPlayerIndex + 1}/{state.Players.Count})");
                    break;
                case DiceBlackjackPhase.DealerTurn:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Orange))
                        ImGui.Text("  Dealer's turn");
                    break;
                case DiceBlackjackPhase.Finished when state.Winner != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  Winner: {PlayerName.Short(state.Winner)}");
                    break;
                default:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                        ImGui.Text("  No active game.");
                    break;
            }
        }
    }

    internal void UpdateWindowConfig() {
        RespectCloseHotkey = Plugin.Config.AllowCloseWithEscape;

        TitleBarButtons.Clear();
        if (Plugin.Config.ShowSettingsButton) {
            TitleBarButtons.Add(new TitleBarButton {
                AvailableClickthrough = false,
                Icon = FontAwesomeIcon.Cog,
                ShowTooltip = () => ImGuiUtil.ToolTip(Language.SettingsTitle),
                Click = _ => Ui.SettingsWindow.Toggle()
            });

            TitleBarButtons.Add(new TitleBarButton() {
                AvailableClickthrough = false,
                Icon = FontAwesomeIcon.Heart,
                ShowTooltip = () => ImGuiUtil.ToolTip("Discord"),
                Click = _ => WindowsApi.OpenUrl("https://discord.gg/BTsHyBzGsN")
            });

#if DEBUG
            TitleBarButtons.Add(new TitleBarButton {
                AvailableClickthrough = false,
                Icon = FontAwesomeIcon.Bug,
                ShowTooltip = () => ImGuiUtil.ToolTip("Debug"),
                Click = _ => Ui.DebugWindow.Toggle()
            });
#endif
        }
    }
}
