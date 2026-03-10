using System;
using System.Collections.Generic;

namespace GameChest;

public enum WordGuessPhase { Idle, Active, Done }

public enum WordGuessVictoryMode { Single, Session }

public record WordGuessRoundResult(int QuestionIndex, string Question, string Answer, string? Winner, DateTime PlayedAt);

public sealed class WordGuessState : IGameState {
    public WordGuessPhase Phase { get; set; } = WordGuessPhase.Idle;
    public bool IsActive => Phase == WordGuessPhase.Active;

    public int CurrentQuestionIndex { get; set; } = -1;
    public bool RoundEnded { get; set; } = false;
    public string? RoundWinner { get; set; }
    public bool HintRevealed { get; set; } = false;

    public DateTime? QuestionStartedAt { get; set; }
    public DateTime? HintRevealAt { get; set; }
    public DateTime? TimerEndsAt { get; set; }

    public TimeSpan TimeRemaining => TimerEndsAt.HasValue
        ? (TimerEndsAt.Value - DateTime.Now).Duration()
        : TimeSpan.Zero;

    // Session mode: fullName → correct answers count
    public Dictionary<string, int> Scores { get; } = new();

    // Rounds completed in this session
    public List<WordGuessRoundResult> SessionRounds { get; } = new();

    public void Reset() {
        Phase = WordGuessPhase.Idle;
        CurrentQuestionIndex = -1;
        RoundEnded = false;
        RoundWinner = null;
        HintRevealed = false;
        QuestionStartedAt = null;
        HintRevealAt = null;
        TimerEndsAt = null;
        Scores.Clear();
        SessionRounds.Clear();
    }

    public void ResetRound() {
        RoundEnded = false;
        RoundWinner = null;
        HintRevealed = false;
        QuestionStartedAt = null;
        HintRevealAt = null;
        TimerEndsAt = null;
    }
}
