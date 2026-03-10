using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;

namespace GameChest;

public record WordGuessResult(string? SessionWinner, int TotalQuestions, List<WordGuessRoundResult> Rounds, DateTime PlayedAt);

public sealed class WordGuessGame : GameBase, IChatConsumer {
    public override string Name => "Word Guess";
    public override GameMode Mode => GameMode.WordGuessGame;
    public override WordGuessState State => _state;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => WordGuessPhraseCategories.All;

    public List<WordGuessResult> MatchHistory { get; } = new();

    private readonly WordGuessState _state = new();
    private Configuration.WordGuessConfiguration Cfg => Plugin.Config.WordGuess;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;

    public WordGuessGame(Plugin plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    // ── IGame ──────────────────────────────────────────────

    public override void Start() {
        if (Cfg.Questions.Count == 0) return;
        _state.Reset();
        _state.CurrentQuestionIndex = 0;
        StartQuestion();
    }

    public override void Stop() {
        if (!_state.IsActive && _state.Phase != WordGuessPhase.Done) return;
        PublishPhrase(WordGuessPhraseCategories.GameCanceled, new Dictionary<string, string>());
        FinishSession(canceled: true);
    }

    public override void Reset() {
        base.Reset();
        MatchHistory.Clear();
    }

    public override void ProcessRoll(Roll roll) { /* not used */ }

    // ── IChatConsumer ──────────────────────────────────────

    public void ProcessChatMessage(string senderFullName, string message, XivChatType chatType) {
        if (_state.Phase != WordGuessPhase.Active) return;
        if (_state.RoundEnded) return;

        var currentQ = CurrentQuestion;
        if (currentQ == null) return;

        if (!IsMatch(message, currentQ.Answer)) return;

        OnCorrectAnswer(senderFullName);
    }

    // ── Game control ───────────────────────────────────────

    /// <summary>Skip or advance to next question depending on round state.</summary>
    public void NextQuestion() {
        if (_state.Phase != WordGuessPhase.Active) return;
        AdvanceQuestion(wasSkipped: !_state.RoundEnded);
    }

    /// <summary>Called from Draw() — checks hint reveal and timer expiry.</summary>
    public void Tick(DateTime now) {
        if (_state.Phase != WordGuessPhase.Active || _state.RoundEnded) return;

        // Hint reveal
        if (Cfg.RevealHint && !_state.HintRevealed
            && _state.HintRevealAt.HasValue && now >= _state.HintRevealAt.Value) {
            _state.HintRevealed = true;
            var currentQ = CurrentQuestion;
            if (currentQ?.Hint != null) {
                PublishPhrase(WordGuessPhraseCategories.HintReveal, new Dictionary<string, string> {
                    ["hint"] = currentQ.Hint,
                    ["number"] = QuestionNumber,
                });
            }
        }

        // Timer expiry
        if (_state.TimerEndsAt.HasValue && now >= _state.TimerEndsAt.Value) {
            OnTimeout();
        }
    }

    public void SimulateAnswer() {
        var q = CurrentQuestion;
        if (q == null || _state.RoundEnded) return;
        ProcessChatMessage("Simulator@Bahamut", q.Answer, XivChatType.Say);
    }

    // ── Internal ───────────────────────────────────────────

    private void StartQuestion() {
        var q = CurrentQuestion;
        if (q == null) { EndSession(); return; }

        _state.Phase = WordGuessPhase.Active;
        _state.ResetRound();
        _state.QuestionStartedAt = DateTime.Now;

        // Timers
        var timerSecs = q.TimerSecs ?? (Cfg.UseGlobalTimer ? Cfg.GlobalTimerSecs : (int?)null);
        if (timerSecs.HasValue && timerSecs.Value > 0) {
            _state.TimerEndsAt = DateTime.Now.AddSeconds(timerSecs.Value);
        }
        if (Cfg.RevealHint && q.Hint != null && Cfg.RevealHintAfterSecs > 0) {
            _state.HintRevealAt = DateTime.Now.AddSeconds(Cfg.RevealHintAfterSecs);
        }

        PublishPhrase(WordGuessPhraseCategories.QuestionStart, new Dictionary<string, string> {
            ["question"] = q.Question,
            ["number"] = QuestionNumber,
            ["total"] = TotalQuestions,
        });
    }

    private void OnCorrectAnswer(string winner) {
        _state.RoundEnded = true;
        _state.RoundWinner = winner;
        _state.TimerEndsAt = null;

        var q = CurrentQuestion!;
        _state.SessionRounds.Add(new WordGuessRoundResult(
            _state.CurrentQuestionIndex, q.Question, q.Answer, winner, DateTime.Now));

        // Session mode score
        if (Cfg.VictoryMode == WordGuessVictoryMode.Session) {
            _state.Scores[winner] = (_state.Scores.TryGetValue(winner, out var s) ? s : 0) + 1;
        }

        PublishPhrase(WordGuessPhraseCategories.RoundWon, new Dictionary<string, string> {
            ["winner"] = Display(winner),
            ["answer"] = q.Answer,
            ["number"] = QuestionNumber,
        });

        if (Cfg.AutoAdvance) {
            AdvanceQuestion(wasSkipped: false);
        }
    }

    private void OnTimeout() {
        _state.RoundEnded = true;
        _state.RoundWinner = null;
        _state.TimerEndsAt = null;

        var q = CurrentQuestion!;
        _state.SessionRounds.Add(new WordGuessRoundResult(
            _state.CurrentQuestionIndex, q.Question, q.Answer, null, DateTime.Now));

        PublishPhrase(WordGuessPhraseCategories.RoundTimeout, new Dictionary<string, string> {
            ["answer"] = q.Answer,
            ["number"] = QuestionNumber,
        });

        if (Cfg.AutoAdvance) {
            AdvanceQuestion(wasSkipped: false);
        }
    }

    private void AdvanceQuestion(bool wasSkipped) {
        if (wasSkipped && _state.Phase == WordGuessPhase.Active && !_state.RoundEnded) {
            // Record skipped round
            var q = CurrentQuestion;
            if (q != null) {
                _state.SessionRounds.Add(new WordGuessRoundResult(
                    _state.CurrentQuestionIndex, q.Question, q.Answer, null, DateTime.Now));
            }
        }

        _state.CurrentQuestionIndex++;
        if (_state.CurrentQuestionIndex >= Cfg.Questions.Count) {
            EndSession();
        } else {
            StartQuestion();
        }
    }

    private void EndSession() {
        _state.Phase = WordGuessPhase.Done;

        if (Cfg.VictoryMode == WordGuessVictoryMode.Session && _state.Scores.Count > 0) {
            var (sessionWinner, score) = _state.Scores.OrderByDescending(kv => kv.Value).First();
            PublishPhrase(WordGuessPhraseCategories.SessionEnd, new Dictionary<string, string> {
                ["winner"] = Display(sessionWinner),
                ["score"] = score.ToString(),
                ["total"] = _state.SessionRounds.Count.ToString(),
            });
            RecordHistory(sessionWinner);
        } else {
            RecordHistory(null);
        }
    }

    private void FinishSession(bool canceled) {
        _state.Phase = WordGuessPhase.Done;
        if (!canceled) RecordHistory(null);
        else _state.Reset();
    }

    private void RecordHistory(string? sessionWinner) {
        var result = new WordGuessResult(
            sessionWinner != null ? Display(sessionWinner) : null,
            Cfg.Questions.Count,
            new List<WordGuessRoundResult>(_state.SessionRounds),
            DateTime.Now);
        MatchHistory.Insert(0, result);
        if (MatchHistory.Count > 10) MatchHistory.RemoveAt(MatchHistory.Count - 1);
    }

    private bool IsMatch(string message, string answer) {
        var comp = Cfg.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return Cfg.AllowPartialMatch
            ? message.Contains(answer.Trim(), comp)
            : string.Equals(message.Trim(), answer.Trim(), comp);
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }

    private static string Display(string fullName) {
        var at = fullName.IndexOf('@');
        return at >= 0 ? fullName[..at] : fullName;
    }

    internal Configuration.WordGuessQuestion? CurrentQuestion =>
        _state.CurrentQuestionIndex >= 0 && _state.CurrentQuestionIndex < Cfg.Questions.Count
            ? Cfg.Questions[_state.CurrentQuestionIndex]
            : null;

    private string QuestionNumber => (_state.CurrentQuestionIndex + 1).ToString();
    private string TotalQuestions => Cfg.Questions.Count.ToString();
}
