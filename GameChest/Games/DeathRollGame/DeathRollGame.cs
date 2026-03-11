using System;
using System.Collections.Generic;

using Dalamud.Game.Text;

namespace GameChest;

public record DeathRollResult(string Winner, string Loser, DateTime PlayedAt);

public sealed class DeathRollGame : GameBase {
    public override string Name => "Death Roll";
    public override GameMode Mode => GameMode.DeathRoll;
    public override DeathRollState State => _state;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => DeathRollPhraseCategories.All;

    public List<DeathRollResult> MatchHistory { get; } = new();

    private readonly DeathRollState _state = new();
    private Configuration.DeathRollConfiguration Cfg => Plugin.Config.DeathRoll;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;
    public override void SetOutputChannel(XivChatType channel) => Cfg.OutputChannel = channel;
    private readonly Random _rng = new();
    private static readonly string[] SimPlayers = { "SimPlayer0@Bahamut", "SimPlayer1@Bahamut" };

    public void SimulateRoll() {
        if (!_state.IsActive) return;
        var outOf = _state.Chain.Count == 0 ? Cfg.StartingRoll : _state.Chain[^1].Result;
        if (outOf <= 1) return;
        var lastPlayer = _state.Chain.Count > 0 ? _state.Chain[^1].PlayerName : SimPlayers[1];
        var player = lastPlayer == SimPlayers[0] ? SimPlayers[1] : SimPlayers[0];
        Plugin.RollManager.ProcessIncomingRollMessage(player, _rng.Next(1, outOf + 1), outOf);
    }

    public DeathRollGame(Plugin plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    public override void Start() {
        _state.Reset();
        _state.Phase = DeathRollPhase.InProgress;
        var vars = new Dictionary<string, string> {
            ["max"] = Cfg.StartingRoll == 999 ? "" : Cfg.StartingRoll.ToString(),
        };
        PublishPhrase(DeathRollPhraseCategories.GameStart, vars);
    }

    public override void Stop() {
        if (!_state.IsActive) return;
        PublishPhrase(DeathRollPhraseCategories.GameCanceled, new Dictionary<string, string>());
        _state.Reset();
    }

    public override void RestartMatch() {
        _state.Chain.Clear();
        _state.Winner = null;
        _state.Loser = null;
        _state.Phase = DeathRollPhase.InProgress;
        var vars = new Dictionary<string, string> {
            ["max"] = Cfg.StartingRoll == 999 ? "" : Cfg.StartingRoll.ToString(),
        };
        PublishPhrase(DeathRollPhraseCategories.GameStart, vars);
    }

    public override void Reset() {
        base.Reset();
        MatchHistory.Clear();
    }

    public override void ProcessRoll(Roll roll) {
        if (_state.Phase != DeathRollPhase.InProgress) return;

        var effective = roll.OutOf == -1 ? 999 : roll.OutOf;

        if (_state.Chain.Count == 0) {
            // First roll: must match configured starting roll
            if (effective != Cfg.StartingRoll) return;
            _state.Chain.Add(new DeathRollEntry(roll.PlayerName, roll.Result, effective, roll.At));
            LogRoll(roll);
            if (roll.Result == 1) EndGame();
            return;
        }

        var last = _state.Chain[^1];
        if (roll.PlayerName == last.PlayerName) return;          // same player twice
        if (effective != last.Result) return;                    // wrong chain value

        _state.Chain.Add(new DeathRollEntry(roll.PlayerName, roll.Result, effective, roll.At));
        LogRoll(roll);

        if (roll.Result == 1) EndGame();
    }

    private void EndGame() {
        var winner = _state.Chain.Count >= 2 ? _state.Chain[^2].PlayerName : null;
        var loser = _state.Chain[^1].PlayerName;

        _state.Winner = winner;
        _state.Loser = loser;
        _state.Phase = DeathRollPhase.Done;

        if (winner != null) {
            var vars = new Dictionary<string, string> {
                ["winner"] = PlayerName.Short(winner),
                ["loser"] = PlayerName.Short(loser),
            };
            PublishPhrase(DeathRollPhraseCategories.GameEnd, vars);
            MatchHistory.Insert(0, new DeathRollResult(PlayerName.Short(winner), PlayerName.Short(loser), DateTime.Now));
            if (MatchHistory.Count > 10) MatchHistory.RemoveAt(MatchHistory.Count - 1);
        }
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }
}
