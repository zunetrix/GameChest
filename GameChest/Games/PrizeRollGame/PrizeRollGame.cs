using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;

using GameChest.Extensions;

namespace GameChest;

public record PrizeRollResult(string Winner, int WinningRoll, int ParticipantCount, PrizeRollSortingMode SortingMode, int NearestTarget, DateTime PlayedAt);

public class PrizeRollGame : GameBase {
    public override string Name => "Prize Roll";
    public override GameMode Mode => GameMode.PrizeRollGame;
    public override PrizeRollState State => _state;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => PrizeRollPhraseCategories.All;

    public List<PrizeRollResult> MatchHistory { get; } = new();

    private readonly PrizeRollState _state = new();
    private Configuration.PrizeRollConfiguration Cfg => Plugin.Config.PrizeRoll;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;
    public override void SetOutputChannel(XivChatType channel) => Cfg.OutputChannel = channel;
    private readonly Random _rng = new();
    private int _simPlayerIdx = 0;

    public void SimulateRoll() {
        if (!_state.IsActive) return;
        var outOf = Cfg.MaxRoll;
        Plugin.RollManager?.ProcessIncomingRollMessage(
            $"Player{++_simPlayerIdx}@Bahamut", _rng.Next(1, outOf + 1), outOf);
    }

    internal PrizeRollGame(IPluginContext plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    public override void Start() {
        RollLog.Clear();
        _state.Start();
        if (Cfg.UseTimer)
            _state.TimerEndsAt = DateTime.Now.AddSeconds(Cfg.TimerDurationSeconds);
        var vars = new Dictionary<string, string> {
            ["max"] = Cfg.MaxRoll == 999 ? "" : Cfg.MaxRoll.ToString(),
            ["mode"] = Cfg.SortingMode.ToString(),
        };
        PublishPhrase(PrizeRollPhraseCategories.GameStart, vars);
    }

    public override void Stop() {
        if (!_state.IsActive) return;

        var winner = _state.Participants.First;
        if (winner != null) {
            var vars = new Dictionary<string, string> {
                ["winner"] = PlayerName.Short(winner.FullName),
                ["roll"] = winner.RollResult.ToString(),
            };
            PublishPhrase(PrizeRollPhraseCategories.GameEnd, vars);
            var count = _state.Participants.Entries.Count;
            MatchHistory.Insert(0, new PrizeRollResult(PlayerName.Short(winner.FullName), winner.RollResult, count, Cfg.SortingMode, Cfg.NearestRoll, DateTime.Now));
            if (MatchHistory.Count > 10) MatchHistory.RemoveAtSafe(MatchHistory.Count - 1);
        }

        _state.Reset();
    }

    public override void Reset() {
        _state.Reset();
    }

    public override void ProcessRoll(Roll roll) {
        if (!_state.IsActive) return;
        var effectiveOutOf = roll.OutOf == -1 ? 999 : roll.OutOf;
        if (effectiveOutOf != Cfg.MaxRoll) return;

        var existing = _state.Participants.Entries.FirstOrDefault(p =>
            string.Equals(p.FullName, roll.PlayerName, StringComparison.OrdinalIgnoreCase));

        if (existing != null) {
            if (!Cfg.RerollAllowed) return;
            _state.Participants.Remove(roll.PlayerName);
        }

        var prevWinner = _state.Participants.First;
        LogRoll(roll);
        var participant = new Participant(roll.PlayerName, roll.Result, roll.OutOf, DateTime.UtcNow);
        _state.Participants.Add(participant);
        Resort();

        var newWinner = _state.Participants.First;
        if (newWinner != null &&
            !string.Equals(newWinner.FullName, prevWinner?.FullName, StringComparison.OrdinalIgnoreCase)) {
            var bestVars = new Dictionary<string, string> {
                ["player"] = PlayerName.Short(newWinner.FullName),
                ["roll"] = newWinner.RollResult.ToString(),
                ["previous"] = prevWinner != null ? PlayerName.Short(prevWinner.FullName) : "",
            };
            PublishPhrase(PrizeRollPhraseCategories.NewBestRoll, bestVars);
        }
    }

    public void Resort() {
        _state.Participants.Entries.Sort((a, b) => Cfg.SortingMode switch {
            PrizeRollSortingMode.Highest => b.RollResult.CompareTo(a.RollResult),
            PrizeRollSortingMode.Lowest => a.RollResult.CompareTo(b.RollResult),
            PrizeRollSortingMode.Nearest =>
                Math.Abs(a.RollResult - Cfg.NearestRoll)
                    .CompareTo(Math.Abs(b.RollResult - Cfg.NearestRoll)),
            _ => 0,
        });
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }
}

