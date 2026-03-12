using Dalamud.Game.Text;

namespace GameChest.Tests;

public class WordGuessGameTests {
    private static (WordGuessGame game, WordGuessState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(c => {
            // Add a default question so Start() works
            c.WordGuess.Questions.Add(new Configuration.WordGuessQuestion {
                Question = "What is 2+2?",
                Answer = "4",
                Enabled = true,
            });
            c.WordGuess.Questions.Add(new Configuration.WordGuessQuestion {
                Question = "What color is the sky?",
                Answer = "blue",
                Enabled = true,
            });
            configure?.Invoke(c);
        });
        var game = new WordGuessGame(ctx);
        return (game, game.State);
    }

    [Fact]
    public void Phase_starts_idle() {
        var (_, state) = Create();
        state.Phase.ShouldBe(WordGuessPhase.Idle);
    }

    [Fact]
    public void Start_sets_Active_phase() {
        var (game, state) = Create();
        game.Start();
        state.Phase.ShouldBe(WordGuessPhase.Active);
    }

    [Fact]
    public void Start_without_enabled_questions_does_not_activate() {
        var ctx = new TestPluginContext(c => {
            c.WordGuess.Questions.Add(new Configuration.WordGuessQuestion {
                Question = "Q", Answer = "A", Enabled = false,
            });
        });
        var game = new WordGuessGame(ctx);
        game.Start();
        game.State.Phase.ShouldBe(WordGuessPhase.Idle);
    }

    [Fact]
    public void Correct_answer_via_chat_wins_round() {
        var (game, state) = Create();
        game.Start();
        game.ProcessChatMessage("PlayerA@Bahamut", "4", XivChatType.Say);
        state.RoundEnded.ShouldBeTrue();
        state.RoundWinner.ShouldBe("PlayerA@Bahamut");
    }

    [Fact]
    public void Wrong_answer_does_not_end_round() {
        var (game, state) = Create();
        game.Start();
        game.ProcessChatMessage("PlayerA@Bahamut", "wrong answer", XivChatType.Say);
        state.RoundEnded.ShouldBeFalse();
    }

    [Fact]
    public void Answer_is_case_insensitive_by_default() {
        var (game, state) = Create();
        game.Start();
        // Jump to question 2 ("blue")
        game.NextQuestion();
        game.ProcessChatMessage("PlayerA@Bahamut", "BLUE", XivChatType.Say);
        state.RoundEnded.ShouldBeTrue();
    }

    [Fact]
    public void Case_sensitive_mode_rejects_wrong_case() {
        var (game, state) = Create(c => {
            c.WordGuess.CaseSensitive = true;
            // Replace questions with a single case-sensitive one
            c.WordGuess.Questions.Clear();
            c.WordGuess.Questions.Add(new Configuration.WordGuessQuestion {
                Question = "Type exactly: Hello",
                Answer = "Hello",
                Enabled = true,
            });
        });
        game.Start();
        game.ProcessChatMessage("PlayerA@Bahamut", "hello", XivChatType.Say); // wrong case
        state.RoundEnded.ShouldBeFalse();
    }

    [Fact]
    public void NextQuestion_advances_to_next_question() {
        var (game, state) = Create();
        game.Start();
        var firstIndex = state.CurrentQuestionIndex;
        game.NextQuestion(); // skip current question
        state.CurrentQuestionIndex.ShouldBeGreaterThan(firstIndex);
    }

    [Fact]
    public void Session_mode_tracks_scores() {
        var (game, state) = Create(c => c.WordGuess.VictoryMode = WordGuessVictoryMode.Session);
        game.Start();
        game.ProcessChatMessage("PlayerA@Bahamut", "4", XivChatType.Say);
        state.Scores.ContainsKey("PlayerA@Bahamut").ShouldBeTrue();
        state.Scores["PlayerA@Bahamut"].ShouldBe(1);
    }

    [Fact]
    public void Stop_cancels_active_game() {
        var (game, state) = Create();
        game.Start();
        game.Stop();
        state.Phase.ShouldBe(WordGuessPhase.Idle);
    }

    [Fact]
    public void All_questions_exhausted_sets_Done_phase() {
        // Create with only one question and AutoAdvance=true
        var ctx = new TestPluginContext(c => {
            c.WordGuess.Questions.Clear();
            c.WordGuess.Questions.Add(new Configuration.WordGuessQuestion {
                Question = "Q1", Answer = "correct", Enabled = true,
            });
            c.WordGuess.AutoAdvance = true;
        });
        var game = new WordGuessGame(ctx);
        game.Start();
        game.ProcessChatMessage("PlayerA@Bahamut", "correct", XivChatType.Say);
        // AutoAdvance with no more questions -> Done
        game.State.Phase.ShouldBe(WordGuessPhase.Done);
    }
}
