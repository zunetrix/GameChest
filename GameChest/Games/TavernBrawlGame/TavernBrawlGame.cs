using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;

namespace GameChest;

public sealed class TavernBrawlGame : GameBase {
    public override string Name => "Tavern Brawl";
    public override GameMode Mode => GameMode.TavernBrawl;
    public override TavernBrawlState State => _state;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => TavernBrawlPhraseCategories.All;

    public List<TavernBrawlResult> MatchHistory { get; } = new();

    private readonly TavernBrawlState _state = new();
    private Configuration.TavernBrawlConfiguration Cfg => Plugin.Config.TavernBrawl;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;

    public TavernBrawlGame(Plugin plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    public void BeginRegistration() {
        _state.Reset();
        _state.Phase = TavernBrawlPhase.Registration;
        PublishPhrase(TavernBrawlPhraseCategories.RegistrationOpen, new Dictionary<string, string>());
    }

    public override void Start() => BeginRegistration();

    public void StartRolling() {
        if (_state.Players.Count < Cfg.MinPlayers) return;
        _state.Phase = TavernBrawlPhase.Rolling;
        _state.Round = 1;
        AnnounceRoundStart();
    }

    public override void Stop() {
        if (!_state.IsActive) return;
        _state.Phase = TavernBrawlPhase.Idle;
        PublishPhrase(TavernBrawlPhraseCategories.GameCanceled, new Dictionary<string, string>());
    }

    public bool TryRegister(string fullName) {
        if (_state.Phase != TavernBrawlPhase.Registration) return false;
        if (_state.Players.Contains(fullName, StringComparer.OrdinalIgnoreCase)) return false;
        _state.Players.Add(fullName);
        return true;
    }

    public void CloseRound() {
        if (_state.Phase != TavernBrawlPhase.Rolling) return;
        if (_state.CurrentRoundRolls.Count == 0) return;

        // Eliminate the lowest roller
        var minRoll = _state.CurrentRoundRolls.Values.Min();
        var loser = _state.CurrentRoundRolls.First(kv => kv.Value == minRoll).Key;
        _state.LowestRoller = loser;
        _state.Players.Remove(loser);
        PublishPhrase(TavernBrawlPhraseCategories.KnockedOut, new Dictionary<string, string> {
            ["player"] = ShortName(loser),
            ["roll"] = minRoll.ToString(),
        });

        if (_state.Players.Count <= 1) {
            EndGame();
            return;
        }

        // Highest roller gets to knock out another
        var maxRoll = _state.CurrentRoundRolls.Values.Max();
        var topRoller = _state.CurrentRoundRolls.First(kv => kv.Value == maxRoll).Key;
        _state.HighestRoller = topRoller;
        _state.HighestRoll = maxRoll;

        PublishPhrase(TavernBrawlPhraseCategories.HighestChooses, new Dictionary<string, string> {
            ["player"] = ShortName(topRoller),
            ["roll"] = maxRoll.ToString(),
        });
        _state.Phase = TavernBrawlPhase.PendingChoice;
    }

    /// <summary>GM selects who the highest roller eliminates.</summary>
    public void EliminateByChoice(string targetName) {
        if (_state.Phase != TavernBrawlPhase.PendingChoice) return;
        _state.Players.Remove(targetName);
        PublishPhrase(TavernBrawlPhraseCategories.KnockedOut, new Dictionary<string, string> {
            ["player"] = ShortName(targetName),
            ["roll"] = "choice",
        });

        if (_state.Players.Count <= 1) {
            EndGame();
            return;
        }

        _state.Round++;
        _state.ResetRound();
        _state.Phase = TavernBrawlPhase.Rolling;
        AnnounceRoundStart();
    }

    public override void ProcessRoll(Roll roll) {
        if (_state.Phase == TavernBrawlPhase.Registration) {
            TryRegister(roll.PlayerName);
            return;
        }

        if (_state.Phase != TavernBrawlPhase.Rolling) return;
        if (roll.OutOf != Cfg.MaxRoll) return;
        if (!_state.Players.Contains(roll.PlayerName, StringComparer.OrdinalIgnoreCase)) return;
        if (_state.CurrentRoundRolls.ContainsKey(roll.PlayerName)) return;

        _state.CurrentRoundRolls[roll.PlayerName] = roll.Result;

        if (_state.CurrentRoundRolls.Count >= _state.Players.Count)
            CloseRound();
    }

    private void AnnounceRoundStart() {
        PublishPhrase(TavernBrawlPhraseCategories.RoundStart, new Dictionary<string, string> {
            ["round"] = _state.Round.ToString(),
            ["maxroll"] = Cfg.MaxRoll.ToString(),
        });
    }

    private void EndGame() {
        var winner = _state.Players.FirstOrDefault();
        _state.Winner = winner;
        _state.Phase = TavernBrawlPhase.Done;
        if (winner != null) {
            PublishPhrase(TavernBrawlPhraseCategories.GameEnd, new Dictionary<string, string> {
                ["winner"] = ShortName(winner),
            });
            MatchHistory.Add(new TavernBrawlResult(winner, _state.Round, DateTime.Now));
            if (MatchHistory.Count > 10) MatchHistory.RemoveAt(0);
        }
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }

    private static string ShortName(string s) { var i = s.IndexOf('@'); return i >= 0 ? s[..i] : s; }
}
