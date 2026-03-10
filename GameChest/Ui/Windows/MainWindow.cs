using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Resources;
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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    private void DrawGameCards() {
        DrawPrizeRollCard();
        ImGui.Spacing();
        DrawFightGameCard();
        ImGui.Spacing();
        DrawDeathRollCard();
        ImGui.Spacing();
        DrawTournamentCard();
        ImGui.Spacing();
        DrawWordGuessCard();
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
            FightPhase.Registration => ("REGISTRATION", Style.Colors.Yellow),
            FightPhase.Initiative => ("INITIATIVE", Style.Colors.Orange),
            FightPhase.Combat => ("COMBAT", Style.Colors.Green),
            FightPhase.Finished => ("FINISHED", Style.Colors.Gray),
            _ => ("IDLE", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.Text($"[{label}]");

        switch (state.Phase) {
            case FightPhase.Combat when state.CurrentAttacker != null && state.PlayerA != null && state.PlayerB != null:
                using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                    ImGui.Text($"  Turn ({state.TurnNumber}): {ShortName(state.CurrentAttacker.FullName)}");
                using (var table = ImRaii.Table("##MHPTable", 2, ImGuiTableFlags.None)) {
                    if (table) {
                        ImGui.TableSetupColumn("##MHPName", ImGuiTableColumnFlags.WidthFixed, 120f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("##MHPBar", ImGuiTableColumnFlags.WidthStretch);
                        DrawMiniHpRow(state.PlayerA, state.CurrentAttacker.FullName == state.PlayerA.FullName);
                        DrawMiniHpRow(state.PlayerB, state.CurrentAttacker.FullName == state.PlayerB.FullName);
                    }
                }
                break;

            case FightPhase.Registration:
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
        var name = ShortName(fighter.FullName);
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
                        ImGui.Text($"  Leading: \uE05D {ShortName(first.FullName)} ({first.RollResult})");
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
                case DeathRollPhase.InProgress:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                        ImGui.Text("[ACTIVE]");
                    ImGui.Text($"  Chain: {state.Chain.Count} roll{(state.Chain.Count != 1 ? "s" : "")}");
                    break;
                case DeathRollPhase.Done:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                        ImGui.Text("[DONE]");
                    if (state.Winner != null)
                        ImGui.Text($"  Winner: {ShortName(state.Winner)}");
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
                DeathRollTournamentPhase.Registration => ("[REGISTRATION]", Style.Colors.Yellow),
                DeathRollTournamentPhase.Preparing => ("[PREPARING]", Style.Colors.Orange),
                DeathRollTournamentPhase.Match => ("[MATCH]", Style.Colors.Green),
                DeathRollTournamentPhase.Done => ("[DONE]", Style.Colors.Blue),
                _ => ("[IDLE]", Style.Colors.Gray),
            };
            using (ImRaii.PushColor(ImGuiCol.Text, cardCol))
                ImGui.Text(cardLabel);

            switch (state.Phase) {
                case DeathRollTournamentPhase.Registration:
                    ImGui.Text($"  {state.RegisteredPlayers.Count} registered");
                    break;
                case DeathRollTournamentPhase.Match when state.MatchPlayer1 != null && state.MatchPlayer2 != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                        ImGui.Text($"  {ShortName(state.MatchPlayer1)} vs {ShortName(state.MatchPlayer2)}");
                    break;
                case DeathRollTournamentPhase.Done when state.TournamentWinner != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                        ImGui.Text($"  Champion: {ShortName(state.TournamentWinner)}");
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
                WordGuessPhase.Done => ("[DONE]", Style.Colors.Blue),
                _ => ("[IDLE]", Style.Colors.Gray),
            };
            using (ImRaii.PushColor(ImGuiCol.Text, cardCol))
                ImGui.Text(cardLabel);

            switch (state.Phase) {
                case WordGuessPhase.Active when wg.CurrentQuestion != null:
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                        ImGui.Text($"  Q {state.CurrentQuestionIndex + 1}/{cfg.Questions.Count}");
                    break;
                case WordGuessPhase.Done:
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

    private static string ShortName(string fullName) {
        var at = fullName.IndexOf('@');
        return at >= 0 ? fullName[..at] : fullName;
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
