using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Extensions;
using GameChest.Resources;

namespace GameChest;

public class FightGameWindow : Window {
    private Plugin Plugin { get; }

    private string _manualRegisterName = string.Empty;

    public FightGameWindow(Plugin plugin)
        : base("Fight Game###FightGameWindow") {
        Plugin = plugin;

        Size = ImGuiHelpers.ScaledVector2(500, 420);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
    }

    public override void Draw() {
        var fg = Plugin.GameManager.FightGame;
        var state = fg.State;

        fg.Notification.Draw();

        using (ImRaii.Group())
            DrawControls(fg, state);

        ImGui.Separator();
        ImGui.Spacing();

        using var tabs = ImRaii.TabBar("##FightTabs");
        if (!tabs) return;

        DrawFightTab(fg, state);
        DrawHistoryTab(fg);
    }

    private void DrawControls(FightGame fg, FightState state) {
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (ImGui.Button("Begin Registration##BeginReg")) {
                fg.BeginRegistration();
            }
        }

        ImGui.SameLine();

        var canStart = state.RegisteredFighters.Count == 2 && state.Phase == FightPhase.Idle;
        using (ImRaii.Disabled(!canStart))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
            if (ImGui.Button("Start Fight##StartFight")) {
                fg.Start();
            }
        }

        ImGui.SameLine();

        using (ImRaii.Disabled(!state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##StopFight"))
                fg.Stop();
        }

        ImGui.SameLine();

        using (ImRaii.Disabled(state.PlayerA == null))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)) {
            if (ImGui.Button("Restart##RestartMatch"))
                fg.RestartMatch();
        }
        ImGuiUtil.ToolTip("Restart match with the same fighters.");

        ImGui.SameLine();

        if (ImGui.Button("Reset##ResetFight") && ImGui.GetIO().KeyCtrl)
            fg.Reset();
        ImGuiUtil.ToolTip("Ctrl+Click to reset.");

        ImGui.SameLine();
        DrawPhaseBadge(state.Phase);
        ImGuiUtil.HelpMarker("""
            Two fighters battle using rolls to attack.
            • Register 2 fighters, then start the fight.
            • Each turn the active fighter rolls /random to attack.
            • The roll value determines damage dealt.
            • Fight ends when a fighter's HP reaches 0.
            """);

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        var btnCount = Plugin.Config.DebugMode ? 3 : 2;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * btnCount + spacing * (btnCount - 1) + marginRight));
        if (Plugin.Config.DebugMode) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.Dice, "##FgSimRoll", "Simulate Roll"))
                fg.SimulateRoll();
            ImGui.SameLine();
        }
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##FgPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.FightGame);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##FgSettings", "Settings"))
            Plugin.Ui.FightGameSettingsWindow.Toggle();
    }

    private static void DrawPhaseBadge(FightPhase phase) {
        var (label, color) = phase switch {
            FightPhase.Registration => ("REGISTRATION", Style.Colors.Yellow),
            FightPhase.Initiative => ("INITIATIVE", Style.Colors.Orange),
            FightPhase.Combat => ("COMBAT", Style.Colors.Green),
            FightPhase.Finished => ("FINISHED", Style.Colors.Gray),
            _ => ("IDLE", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.Text($"[{label}]");
    }

    private void DrawFightTab(FightGame fg, FightState state) {
        using var tabItem = ImRaii.TabItem("Fight##FgFightTab");
        if (!tabItem) return;

        ImGui.Spacing();

        switch (state.Phase) {
            case FightPhase.Registration:
            case FightPhase.Idle:
                DrawRegistrationSection(fg, state);
                break;
            case FightPhase.Initiative:
                DrawInitiativeSection(state);
                break;
            case FightPhase.Combat:
                DrawCombatSection(state);
                break;
            case FightPhase.Finished:
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                    ImGui.Text("Fight finished. Click \"Begin Registration\" to play again.");
                break;
            default:
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                    ImGui.Text("No active fight. Click \"Begin Registration\" to start.");
                break;
        }

        if (fg.RollLog.Count > 0) {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.BeginChild("##FgRollLogChild", new Vector2(-1, -1), false);
            RollLogTable.Draw("##FgRollLog", fg.RollLog, Plugin.Config.HighlightColor);
            ImGui.EndChild();
        }
    }

    private static void DrawHistoryTab(FightGame fg) {
        using var tabItem = ImRaii.TabItem("History##FgHistoryTab");
        if (!tabItem) return;

        ImGui.Spacing();

        if (fg.MatchHistory.Count == 0) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text("No match history yet.");
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
        .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
        .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "##FgClearHistory", "Ctrl+Click to clear history") && ImGui.GetIO().KeyCtrl)
                fg.MatchHistory.Clear();
        }

        if (fg.MatchHistory.Count == 0) return;

        ImGui.Spacing();

        var scale = ImGuiHelpers.GlobalScale;
        using var table = ImRaii.Table("##FgHistoryTable", 5,
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30f * scale);
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 55f * scale);
        ImGui.TableSetupColumn("Winner", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Loser", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("HP", ImGuiTableColumnFlags.WidthFixed, 50f * scale);
        ImGui.TableHeadersRow();

        for (var i = 0; i < fg.MatchHistory.Count; i++) {
            var r = fg.MatchHistory[i];
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text($"{i + 1}");
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text(r.PlayedAt.ToString("HH:mm"));
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                ImGui.Text(ShortName(r.Winner));
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Red))
                ImGui.Text(ShortName(r.Loser));
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text($"{r.WinnerHp}");
        }
    }

    private void DrawRegistrationSection(FightGame fg, FightState state) {
        if (state.RegisteredFighters.Count < 2) {
            ImGui.SetNextItemWidth(350f * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint("##RegName", "Firstname Lastname[@World]", ref _manualRegisterName, 100);

            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
                if (ImGui.Button($"{Language.Add}##ManualReg") && !string.IsNullOrWhiteSpace(_manualRegisterName)) {
                    fg.TryRegister(_manualRegisterName.Trim(), RegistrationSource.Manual);
                    _manualRegisterName = string.Empty;
                }
            }

            ImGui.SameLine();
            if (ImGuiUtil.IconButton(FontAwesomeIcon.Crosshairs, "##TargetReg", "Register targeted player")) {
                var target = DalamudApi.TargetManager.Target;
                if (target != null)
                    fg.TryRegister(target.Name.TextValue, RegistrationSource.Target);
            }

            ImGui.Spacing();
        }

        ImGui.Text("Registered Fighters:");
        ImGui.Spacing();

        var scale = ImGuiHelpers.GlobalScale;
        var btnW = ImGui.GetFrameHeight();
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        using var table = ImRaii.Table("##FgRegTable", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("##FgRegLabel", ImGuiTableColumnFlags.WidthFixed, 28f * scale);
        ImGui.TableSetupColumn("##FgRegName", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##FgRegBtns", ImGuiTableColumnFlags.WidthFixed, btnW * 2 + spacing);

        for (var i = 0; i < 2; i++) {
            var occupied = i < state.RegisteredFighters.Count;
            var fighter = occupied ? state.RegisteredFighters[i] : null;

            ImGui.PushID(i);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text($"{(char)('A' + i)}.");

            ImGui.TableNextColumn();
            if (occupied) {
                ImGui.Text(ShortName(fighter!.FullName));
                ImGui.SameLine();
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                    ImGui.Text($"({fighter.Source})");
            } else {
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                    ImGui.Text("(empty slot)");
            }

            ImGui.TableNextColumn();
            using (ImRaii.Disabled(!occupied))
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Times, "##Rem", "Remove"))
                    state.RegisteredFighters.RemoveAtSafe(i);
                ImGui.SameLine();
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Ban, "##Blk", $"{Language.Block} (Ctrl+Click)")
                    && ImGui.GetIO().KeyCtrl && fighter != null) {
                    var fullName = fighter.FullName;
                    state.RegisteredFighters.RemoveAtSafe(i);
                    if (!Plugin.Config.Blocklist.ContainsPlayer(fullName)) {
                        Plugin.Config.Blocklist.Add(fullName);
                        Plugin.Config.Save();
                    }
                }
            }

            ImGui.PopID();
        }
    }

    private static void DrawInitiativeSection(FightState state) {
        if (state.PlayerA == null || state.PlayerB == null) return;

        ImGui.Text("Rolling for initiative...");
        ImGui.Spacing();
        DrawInitiativeRow(state.PlayerA.FullName, state.InitiativeRollA);
        DrawInitiativeRow(state.PlayerB.FullName, state.InitiativeRollB);
    }

    private static void DrawInitiativeRow(string fullName, int? roll) {
        ImGui.Text($"  {ShortName(fullName)}");
        ImGui.SameLine();
        if (roll.HasValue) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                ImGui.Text($"\u2192 {roll.Value}");
        } else {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text("(waiting for roll...)");
        }
    }

    private void DrawCombatSection(FightState state) {
        if (state.PlayerA == null || state.PlayerB == null) return;

        if (state.CurrentAttacker != null) {
            using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                ImGui.Text($"  Turn ({state.TurnNumber}): {ShortName(state.CurrentAttacker.FullName)}");
            ImGui.Spacing();
        }

        var hasMP = state.PlayerA.MaxMp > 0 || state.PlayerB.MaxMp > 0;
        var cols = hasMP ? 4 : 3;

        using var table = ImRaii.Table("##Fighters", cols, ImGuiTableFlags.None);
        if (!table) return;

        ImGui.TableSetupColumn("##FName", ImGuiTableColumnFlags.WidthFixed, 160f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##FHP", ImGuiTableColumnFlags.WidthFixed, 180f * ImGuiHelpers.GlobalScale);
        if (hasMP)
            ImGui.TableSetupColumn("##FMP", ImGuiTableColumnFlags.WidthFixed, 110f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##FStatus", ImGuiTableColumnFlags.WidthStretch);

        DrawFighterRow(state.PlayerA, state.CurrentAttacker?.FullName == state.PlayerA.FullName, hasMP);
        DrawFighterRow(state.PlayerB, state.CurrentAttacker?.FullName == state.PlayerB.FullName, hasMP);
    }

    private void DrawFighterRow(FighterState fighter, bool isCurrentTurn, bool hasMP) {
        var hpFrac = fighter.MaxHealth > 0 ? (float)fighter.Health / fighter.MaxHealth : 0f;
        var hpColor = hpFrac > 0.5f ? Style.Colors.GrassGreen
                    : hpFrac > 0.2f ? Style.Colors.Orange
                    : Style.Colors.Red;

        ImGui.TableNextColumn();
        var name = ShortName(fighter.FullName);
        if (isCurrentTurn) {
            using (ImRaii.PushColor(ImGuiCol.Text, Plugin.Config.HighlightColor))
                ImGui.Text($"\uE05D {name}");
        } else {
            ImGui.Text($"  {name}");
        }

        ImGui.TableNextColumn();
        using (ImRaii.PushColor(ImGuiCol.PlotHistogram, hpColor))
            ImGui.ProgressBar(hpFrac, new Vector2(-1, 0), $"{fighter.Health}/{fighter.MaxHealth} HP");

        if (hasMP) {
            ImGui.TableNextColumn();
            if (fighter.MaxMp > 0) {
                var mpFrac = (float)fighter.Mp / fighter.MaxMp;
                using (ImRaii.PushColor(ImGuiCol.PlotHistogram, Style.Colors.Blue))
                    ImGui.ProgressBar(mpFrac, new Vector2(-1, 0), $"{fighter.Mp}/{fighter.MaxMp} MP");
            }
        }

        ImGui.TableNextColumn();
        if (fighter.SkipNextTurn) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Red))
                ImGui.Text("[STUNNED]");
        }
    }

    private static string ShortName(string fullName) {
        var at = fullName.IndexOf('@');
        return at >= 0 ? fullName[..at] : fullName;
    }
}
