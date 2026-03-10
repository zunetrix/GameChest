using System;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class WordGuessWindow : Window {
    private Plugin Plugin { get; }

    public WordGuessWindow(Plugin plugin) : base("Word Guess###WordGuessWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(480, 420);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var wg = Plugin.GameManager.WordGuessGame;
        var state = wg.State;

        // Tick timers (hint reveal + question timeout)
        if (state.IsActive) wg.Tick(DateTime.Now);

        wg.Notification.Draw();

        using (ImRaii.Group())
            DrawControls(wg, state);

        ImGui.Separator();
        ImGui.Spacing();

        using var tabs = ImRaii.TabBar("##WgTabs");
        if (!tabs) return;

        DrawGameTab(wg, state);
        DrawHistoryTab(wg);
    }

    private void DrawControls(WordGuessGame wg, WordGuessState state) {
        var cfg = Plugin.Config.WordGuess;
        var noQuestions = !cfg.Questions.Any(q => q.Enabled);

        // Start (enabled when Idle or Done)
        using (ImRaii.Disabled(state.IsActive || noQuestions))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (ImGui.Button("Start##WgStart")) wg.Start();
        }
        if (noQuestions) ImGuiUtil.ToolTip("Enable at least one question in the Question List first.");

        // Next / Skip (only when Active)
        if (state.IsActive) {
            ImGui.SameLine();
            var nextLabel = state.RoundEnded ? "Next##WgNext" : "Skip##WgSkip";
            if (ImGui.Button(nextLabel)) wg.NextQuestion();
        }

        ImGui.SameLine();

        // Stop (enabled when Active)
        using (ImRaii.Disabled(!state.IsActive))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGui.Button("Stop##WgStop")) wg.Stop();
        }

        ImGui.SameLine();
        DrawPhaseBadge(state);
        ImGuiUtil.HelpMarker("""
            Players race to type the correct answer in chat.
            • GM starts a session - questions are announced one by one.
            • First player to type the exact answer wins the round.
            • Single mode: each round has its own winner.
            • Session mode: most correct answers across all rounds wins.
            • GM can Skip a question or Stop the session at any time.
            """);

        // Right-aligned icon group
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnW = ImGui.GetFrameHeight();
        float marginRight = 15f * ImGuiHelpers.GlobalScale;
        var btnCount = Plugin.Config.DebugMode ? 4 : 3;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (btnW * btnCount + spacing * (btnCount - 1) + marginRight));

        if (Plugin.Config.DebugMode) {
            using (ImRaii.Disabled(!state.IsActive || state.RoundEnded))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Dice, "##WgSimAnswer", "Simulate correct answer"))
                    wg.SimulateAnswer();
            ImGui.SameLine();
        }
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Book, "##WgQuestions", "Question List"))
            Plugin.Ui.WordGuessQuestionListWindow.Toggle();
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.ClipboardList, "##WgPhrases", "Phrases"))
            Plugin.Ui.GamePhrasesWindow.OpenToGame(GameMode.WordGuessGame);
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Cog, "##WgSettings", "Settings"))
            Plugin.Ui.WordGuessSettingsWindow.Toggle();
    }

    private void DrawGameTab(WordGuessGame wg, WordGuessState state) {
        using var tabItem = ImRaii.TabItem("Game##WgGameTab");
        if (!tabItem) return;

        var cfg = Plugin.Config.WordGuess;

        if (!state.IsActive && state.Phase == WordGuessPhase.Idle) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text(!cfg.Questions.Any(q => q.Enabled)
                    ? "No enabled questions. Open the Question List to configure some."
                    : $"{cfg.Questions.Count(q => q.Enabled)} enabled question(s) ready. Click Start to begin.");
            return;
        }

        // Current question block
        var q = wg.CurrentQuestion;
        if (q != null) {
            ImGui.Spacing();

            // Q index header
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Yellow))
                ImGui.Text($"Q {cfg.Questions.Take(state.CurrentQuestionIndex + 1).Count(x => x.Enabled)} / {cfg.Questions.Count(x => x.Enabled)}");

            ImGui.Spacing();

            // Question text
            ImGui.Text("Question:");
            ImGui.SameLine();
            ImGui.TextWrapped(q.Question);

            // Answer line with manual send button
            ImGui.Text("Answer:  ");
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, state.RoundEnded ? Style.Colors.Green : Plugin.Config.HighlightColor))
                ImGui.Text(q.Answer);
            ImGui.SameLine();
            using (ImRaii.Disabled(!state.IsActive || state.RoundEnded))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.PaperPlane, "##WgSendAnswer", "Send answer to chat"))
                    wg.PublishAnswer();

            // Hint row with manual send button
            if (q.Hint != null) {
                ImGui.Text("Hint:    ");
                ImGui.SameLine();
                if (state.HintRevealed) {
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                        ImGui.Text($"Sent - {q.Hint}");
                } else if (!cfg.RevealHint) {
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                        ImGui.Text(q.Hint);
                } else if (state.HintRevealAt.HasValue) {
                    var secs = (int)Math.Ceiling((state.HintRevealAt.Value - DateTime.Now).TotalSeconds);
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                        ImGui.Text($"Reveals in {Math.Max(0, secs)}s");
                }
                ImGui.SameLine();
                using (ImRaii.Disabled(!state.IsActive || state.RoundEnded || state.HintRevealed))
                    if (ImGuiUtil.IconButton(FontAwesomeIcon.Lightbulb, "##WgSendHint", "Send hint to chat now"))
                        wg.PublishHint();
            }

            // Timer row
            var timerSecs = q.TimerSecs ?? (cfg.UseGlobalTimer ? cfg.GlobalTimerSecs : (int?)null);
            if (timerSecs.HasValue) {
                ImGui.Spacing();
                DrawTimerRow(wg, state, timerSecs.Value);
            }

            // Round result banner
            if (state.RoundEnded) {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                if (state.RoundWinner != null) {
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                        ImGui.Text($"Winner: {ShortName(state.RoundWinner)}");
                } else {
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Red))
                        ImGui.Text("No winner (timeout / skipped)");
                }
                using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                    ImGui.Text("Press Next to continue.");
            }
        } else if (state.Phase == WordGuessPhase.Done) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                ImGui.Text("Session complete!");
        }

        // Session mode scores
        if (cfg.VictoryMode == WordGuessVictoryMode.Session && state.Scores.Count > 0) {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Text("Session Scores:");
            ImGui.Spacing();

            var ranked = state.Scores.OrderByDescending(kv => kv.Value).ToList();
            if (ImGui.BeginTable("##WgScores", 3,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings)) {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Correct", ImGuiTableColumnFlags.WidthFixed, 55 * ImGuiHelpers.GlobalScale);
                ImGui.TableHeadersRow();

                for (var i = 0; i < ranked.Count; i++) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{i + 1}");
                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, i == 0 ? Plugin.Config.HighlightColor : Style.Colors.White))
                        ImGui.Text(ShortName(ranked[i].Key));
                    ImGui.TableNextColumn();
                    ImGui.Text($"{ranked[i].Value}");
                }
                ImGui.EndTable();
            }
        }

        // Round history within this session
        if (state.SessionRounds.Count > 0) {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Text("This session:");
            ImGui.Spacing();

            if (ImGui.BeginTable("##WgSessionRounds", 3,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings)) {
                ImGui.TableSetupColumn("Q", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Answer", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Winner", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var r in state.SessionRounds) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                        ImGui.Text($"{r.QuestionIndex + 1}");
                    ImGui.TableNextColumn();
                    ImGui.Text(r.Answer);
                    ImGui.TableNextColumn();
                    if (r.Winner != null) {
                        using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                            ImGui.Text(ShortName(r.Winner));
                    } else {
                        using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Red))
                            ImGui.Text("-");
                    }
                }
                ImGui.EndTable();
            }
        }
    }

    private void DrawHistoryTab(WordGuessGame wg) {
        using var tabItem = ImRaii.TabItem("History##WgHistoryTab");
        if (!tabItem) return;

        if (wg.MatchHistory.Count == 0) {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text("No session history yet.");
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "##WgClearHistory", "Ctrl+Click to clear history")
                && ImGui.GetIO().KeyCtrl)
                wg.MatchHistory.Clear();
        }
        ImGui.Spacing();

        for (var i = 0; i < wg.MatchHistory.Count; i++) {
            var r = wg.MatchHistory[i];
            using var id = ImRaii.PushId(i);

            var label = r.SessionWinner != null
                ? $"Session {i + 1}  -  {r.PlayedAt:HH:mm}  -  Winner: {r.SessionWinner}"
                : $"Session {i + 1}  -  {r.PlayedAt:HH:mm}  -  {r.Rounds.Count}/{r.TotalQuestions} rounds";

            if (ImGui.CollapsingHeader(label)) {
                if (ImGui.BeginTable($"##WgHist{i}", 3,
                    ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings)) {
                    ImGui.TableSetupColumn("Q", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Answer", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Winner", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    foreach (var rnd in r.Rounds) {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                            ImGui.Text($"{rnd.QuestionIndex + 1}");
                        ImGui.TableNextColumn();
                        ImGui.Text(rnd.Answer);
                        ImGui.TableNextColumn();
                        if (rnd.Winner != null) {
                            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green))
                                ImGui.Text(ShortName(rnd.Winner));
                        } else {
                            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Red))
                                ImGui.Text("-");
                        }
                    }
                    ImGui.EndTable();
                }
            }
        }
    }

    private static void DrawTimerRow(WordGuessGame wg, WordGuessState state, int timerSecs) {
        if (state.TimerEndsAt.HasValue) {
            var rem = state.TimeRemaining;
            var color = rem.TotalSeconds < 10 ? Style.Colors.Red
                      : rem.TotalSeconds < 20 ? Style.Colors.Orange
                      : Style.Colors.Green;
            using (ImRaii.PushColor(ImGuiCol.Text, color))
                ImGui.Text($"Time remaining: {(int)rem.TotalMinutes:D2}:{rem.Seconds:D2}");
            ImGui.SameLine();
            using (ImRaii.Disabled(state.RoundEnded))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Stop, "##WgStopTimer", "Stop timer"))
                    wg.StopTimer();
        } else {
            var m = timerSecs / 60;
            var s = timerSecs % 60;
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text($"Timer: {m:D2}:{s:D2}  (not started)");
            ImGui.SameLine();
            using (ImRaii.Disabled(state.RoundEnded))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Play, "##WgStartTimer", "Start timer"))
                    wg.StartTimer();
        }
    }

    private static void DrawPhaseBadge(WordGuessState state) {
        var (label, color) = state.Phase switch {
            WordGuessPhase.Active => ("[ACTIVE]", Style.Colors.Green),
            WordGuessPhase.Done => ("[DONE]", Style.Colors.Gray),
            _ => ("[IDLE]", Style.Colors.Gray),
        };
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.Text(label);
    }

    private static string ShortName(string fullName) {
        var at = fullName.IndexOf('@');
        return at >= 0 ? fullName[..at] : fullName;
    }
}
