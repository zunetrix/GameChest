using System;
using System.Collections.Generic;

namespace GameChest;

public static class WordGuessPhraseCategories {
    public const string QuestionStart = "QuestionStart";
    public const string HintReveal = "HintReveal";
    public const string RoundWon = "RoundWon";
    public const string RoundTimeout = "RoundTimeout";
    public const string SessionEnd = "SessionEnd";
    public const string GameCanceled = "GameCanceled";

    public static readonly IReadOnlyList<PhraseCategoryMeta> All = new List<PhraseCategoryMeta> {
        new(QuestionStart, "Question Start", new[] { "{question}", "{number}", "{total}" }, new[] {
            "Round {number}/{total} — {question}",
            "Question {number} of {total}: {question}",
        }),
        new(HintReveal, "Hint Reveal", new[] { "{hint}", "{number}" }, new[] {
            "💡 Hint for round {number}: {hint}",
            "Hint: {hint}",
        }),
        new(RoundWon, "Round Won", new[] { "{winner}", "{answer}", "{number}" }, new[] {
            "{winner} got it! The answer was: {answer}",
            "Correct! {winner} wins round {number}! The answer was: {answer}",
            "{winner} takes round {number} with: {answer}",
        }),
        new(RoundTimeout, "Round Timeout", new[] { "{answer}", "{number}" }, new[] {
            "Time's up! The answer was: {answer}",
            "Nobody guessed it! Round {number} answer: {answer}",
        }),
        new(SessionEnd, "Session End", new[] { "{winner}", "{score}", "{total}" }, new[] {
            "{winner} wins the session with {score} correct answer(s) out of {total}!",
            "Session over! {winner} is the champion with {score}/{total}!",
        }),
        new(GameCanceled, "Game Canceled", Array.Empty<string>(), new[] {
            "Word Guess has been cancelled.",
        }, false),
    };
}
