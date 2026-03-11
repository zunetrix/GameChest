using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;

namespace GameChest;

public sealed class HighRollDuelGame : GameBase {
    public override string Name => "High Roll Duel";
    public override GameMode Mode => GameMode.HighRollDuel;
    public override HighRollDuelState State => _state;
    public override bool IsRegistering => _state.Phase == HighRollDuelPhase.Registration;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => HighRollDuelPhraseCategories.All;

    public List<HighRollDuelResult> MatchHistory { get; } = new();

    private readonly HighRollDuelState _state = new();
    private readonly Random _rng = new();
    private int _simPlayerIdx;
    private Configuration.HighRollDuelConfiguration Cfg => Plugin.Config.HighRollDuel;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;
    public override void SetOutputChannel(XivChatType channel) => Cfg.OutputChannel = channel;

    public HighRollDuelGame(Plugin plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    public void BeginRegistration() {
        _state.Reset();
        _state.Phase = HighRollDuelPhase.Registration;
        PublishPhrase(HighRollDuelPhraseCategories.RegistrationOpen, new Dictionary<string, string>());
    }

    public override void Start() => BeginRegistration();

    public void StartRolling() {
        if (_state.Players.Count < Cfg.MinPlayers) return;
        _state.Phase = HighRollDuelPhase.Rolling;
        _state.Round = 1;
        AnnounceRoundStart();
    }

    public override void Stop() {
        if (!_state.IsActive) return;
        _state.Phase = HighRollDuelPhase.Idle;
        PublishPhrase(HighRollDuelPhraseCategories.GameCanceled, new Dictionary<string, string>());
    }

    public bool TryRegister(string fullName) {
        if (_state.Phase != HighRollDuelPhase.Registration) return false;
        if (_state.Players.Contains(fullName, StringComparer.OrdinalIgnoreCase)) return false;
        _state.Players.Add(fullName);
        return true;
    }

    public void CloseRound() {
        if (_state.Phase != HighRollDuelPhase.Rolling) return;
        if (_state.CurrentRoundRolls.Count == 0) return;

        var minRoll = _state.CurrentRoundRolls.Values.Min();
        var losers = _state.CurrentRoundRolls
            .Where(kv => kv.Value == minRoll)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var loser in losers) {
            _state.Players.Remove(loser);
            _state.RoundEliminations.Add(loser);
            PublishPhrase(HighRollDuelPhraseCategories.PlayerEliminated, new Dictionary<string, string> {
                ["player"] = ShortName(loser),
                ["roll"] = minRoll.ToString(),
            });
        }

        if (_state.Players.Count <= 1) {
            EndGame();
        } else {
            _state.Round++;
            _state.ResetRound();
            AnnounceRoundStart();
        }
    }

    public void SimulateRoll() {
        var outOf = Cfg.MaxRoll;
        if (_state.Phase == HighRollDuelPhase.Registration) {
            Plugin.RollManager.ProcessIncomingRollMessage(
                $"Player{++_simPlayerIdx}@Bahamut", _rng.Next(1, outOf + 1), outOf);
        } else if (_state.Phase == HighRollDuelPhase.Rolling) {
            var pending = _state.Players.FirstOrDefault(p => !_state.CurrentRoundRolls.ContainsKey(p));
            if (pending != null)
                Plugin.RollManager.ProcessIncomingRollMessage(pending, _rng.Next(1, outOf + 1), outOf);
        }
    }

    public override void ProcessRoll(Roll roll) {
        if (_state.Phase == HighRollDuelPhase.Registration) {
            TryRegister(roll.PlayerName);
            return;
        }

        if (_state.Phase != HighRollDuelPhase.Rolling) return;
        if (roll.OutOf != Cfg.MaxRoll) return;
        if (!_state.Players.Contains(roll.PlayerName, StringComparer.OrdinalIgnoreCase)) return;
        if (_state.CurrentRoundRolls.ContainsKey(roll.PlayerName)) return;

        _state.CurrentRoundRolls[roll.PlayerName] = roll.Result;

        if (Cfg.AutoCloseRound && _state.CurrentRoundRolls.Count >= _state.Players.Count)
            CloseRound();
    }

    private void AnnounceRoundStart() {
        PublishPhrase(HighRollDuelPhraseCategories.RoundStart, new Dictionary<string, string> {
            ["round"] = _state.Round.ToString(),
            ["maxroll"] = Cfg.MaxRoll.ToString(),
        });
    }

    private void EndGame() {
        var winner = _state.Players.FirstOrDefault();
        _state.Winner = winner;
        _state.Phase = HighRollDuelPhase.Done;
        if (winner != null) {
            PublishPhrase(HighRollDuelPhraseCategories.GameEnd, new Dictionary<string, string> {
                ["winner"] = ShortName(winner),
            });
            MatchHistory.Add(new HighRollDuelResult(winner, _state.Round, DateTime.Now));
            if (MatchHistory.Count > 10) MatchHistory.RemoveAt(0);
        }
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }

    private static string ShortName(string s) { var i = s.IndexOf('@'); return i >= 0 ? s[..i] : s; }
}
