using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;

namespace GameChest;

public class KingOfTheHillGame : GameBase {
    public override string Name => "King of the Hill";
    public override GameMode Mode => GameMode.KingOfTheHill;
    public override KingOfTheHillState State => _state;
    public override bool IsRegistering => _state.Phase == KingOfTheHillPhase.Registering;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => KingOfTheHillPhraseCategories.All;

    public List<KingOfTheHillResult> MatchHistory { get; } = new();

    private readonly KingOfTheHillState _state = new();
    private readonly Random _rng = new();
    private int _simPlayerIdx;
    private Configuration.KingOfTheHillConfiguration Cfg => Plugin.Config.KingOfTheHill;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;
    public override void SetOutputChannel(XivChatType channel) => Cfg.OutputChannel = channel;

    internal KingOfTheHillGame(IPluginContext plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    public void BeginRegistration() {
        _state.Reset();
        _state.Phase = KingOfTheHillPhase.Registering;
        PublishPhrase(KingOfTheHillPhraseCategories.RegistrationOpen, new Dictionary<string, string>());
    }

    public override void Start() => BeginRegistration();

    public void StartRolling() {
        if (_state.Players.Count < Cfg.MinPlayers) return;
        _state.Phase = KingOfTheHillPhase.Rolling;
        _state.Round = 1;
        AnnounceRoundStart();
    }

    public override void Stop() {
        if (!_state.IsActive) return;
        _state.Phase = KingOfTheHillPhase.Idle;
        PublishPhrase(KingOfTheHillPhraseCategories.GameCanceled, new Dictionary<string, string>());
    }

    public override bool TryJoin(string fullName, JoinSource source) {
        if (_state.Phase != KingOfTheHillPhase.Registering) return false;
        if (_state.Players.Contains(fullName, StringComparer.OrdinalIgnoreCase)) return false;
        _state.Players.Add(fullName);
        return true;
    }

    public void CloseRound() {
        if (_state.Phase != KingOfTheHillPhase.Rolling) return;
        if (_state.CurrentRoundRolls.Count == 0) return;

        var maxRoll = _state.CurrentRoundRolls.Values.Max();
        var topRoller = _state.CurrentRoundRolls.OrderByDescending(kv => kv.Value).First().Key;

        if (_state.King == null || !topRoller.Equals(_state.King, StringComparison.OrdinalIgnoreCase)) {
            // New king
            _state.King = topRoller;
            _state.KingHoldCount = 1;
            PublishPhrase(KingOfTheHillPhraseCategories.NewKing, new Dictionary<string, string> {
                ["player"] = PlayerName.Short(topRoller), ["roll"] = maxRoll.ToString(),
            });
        } else {
            // King defends
            _state.KingHoldCount++;
            var vars = new Dictionary<string, string> {
                ["king"] = PlayerName.Short(_state.King),
                ["roll"] = maxRoll.ToString(),
                ["holds"] = _state.KingHoldCount.ToString(),
                ["target"] = Cfg.CrownHoldRounds.ToString(),
            };
            PublishPhrase(KingOfTheHillPhraseCategories.KingDefends, vars);
        }

        if (_state.KingHoldCount >= Cfg.CrownHoldRounds) {
            EndGame();
        } else {
            _state.Round++;
            _state.ResetRound();
            AnnounceRoundStart();
        }
    }

    public void SimulateRoll() {
        var outOf = Cfg.MaxRoll;
        if (_state.Phase == KingOfTheHillPhase.Registering) {
            Plugin.RollManager?.ProcessIncomingRollMessage(
                $"Player{++_simPlayerIdx}@Bahamut", _rng.Next(1, outOf + 1), outOf);
        } else if (_state.Phase == KingOfTheHillPhase.Rolling) {
            var pending = _state.Players.FirstOrDefault(p => !_state.CurrentRoundRolls.ContainsKey(p));
            if (pending != null)
                Plugin.RollManager?.ProcessIncomingRollMessage(pending, _rng.Next(1, outOf + 1), outOf);
        }
    }

    public override void ProcessRoll(Roll roll) {
        if (_state.Phase == KingOfTheHillPhase.Registering) { TryJoin(roll.PlayerName, JoinSource.Roll); return; }
        if (_state.Phase != KingOfTheHillPhase.Rolling) return;
        if (roll.OutOf != Cfg.MaxRoll) return;
        if (!_state.Players.Contains(roll.PlayerName, StringComparer.OrdinalIgnoreCase)) return;
        if (_state.CurrentRoundRolls.ContainsKey(roll.PlayerName)) return;

        _state.CurrentRoundRolls[roll.PlayerName] = roll.Result;

        if (_state.CurrentRoundRolls.Count >= _state.Players.Count)
            CloseRound();
    }

    private void AnnounceRoundStart() {
        PublishPhrase(KingOfTheHillPhraseCategories.RoundStart, new Dictionary<string, string> {
            ["round"] = _state.Round.ToString(),
            ["maxroll"] = Cfg.MaxRoll.ToString(),
            ["king"] = _state.King != null ? PlayerName.Short(_state.King) : "none",
            ["holds"] = _state.KingHoldCount.ToString(),
            ["target"] = Cfg.CrownHoldRounds.ToString(),
        });
    }

    private void EndGame() {
        _state.Winner = _state.King;
        _state.Phase = KingOfTheHillPhase.Finished;
        if (_state.King != null) {
            PublishPhrase(KingOfTheHillPhraseCategories.GameEnd, new Dictionary<string, string> {
                ["winner"] = PlayerName.Short(_state.King),
                ["holds"] = _state.KingHoldCount.ToString(),
            });
            MatchHistory.Add(new KingOfTheHillResult(_state.King, _state.Round, DateTime.Now));
            if (MatchHistory.Count > 10) MatchHistory.RemoveAt(0);
        }
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }

}
