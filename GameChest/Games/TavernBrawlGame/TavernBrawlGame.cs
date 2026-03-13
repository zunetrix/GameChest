using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;

namespace GameChest;

public class TavernBrawlGame : GameBase, IChatConsumer {
    public override string Name => "Tavern Brawl";
    public override GameMode Mode => GameMode.TavernBrawl;
    public override TavernBrawlState State => _state;
    public override bool IsRegistering => _state.Phase == TavernBrawlPhase.Registration;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => TavernBrawlPhraseCategories.All;

    public List<TavernBrawlResult> MatchHistory { get; } = new();

    private readonly TavernBrawlState _state = new();
    private readonly Random _rng = new();
    private int _simPlayerIdx;
    private Configuration.TavernBrawlConfiguration Cfg => Plugin.Config.TavernBrawl;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;
    public override void SetOutputChannel(XivChatType channel) => Cfg.OutputChannel = channel;

    internal TavernBrawlGame(IPluginContext plugin) : base(plugin) {
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

    public override bool TryJoin(string fullName, JoinSource source) {
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
            ["player"] = PlayerName.Short(loser),
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
            ["player"] = PlayerName.Short(topRoller),
            ["roll"] = maxRoll.ToString(),
        });
        _state.Phase = TavernBrawlPhase.PendingChoice;
    }

    /// <summary>GM selects who the highest roller eliminates.</summary>
    public void EliminateByChoice(string targetName) {
        if (_state.Phase != TavernBrawlPhase.PendingChoice) return;
        _state.Players.Remove(targetName);
        PublishPhrase(TavernBrawlPhraseCategories.KnockedOut, new Dictionary<string, string> {
            ["player"] = PlayerName.Short(targetName),
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

    public void SimulateRoll() {
        var outOf = Cfg.MaxRoll;
        if (_state.Phase == TavernBrawlPhase.Registration) {
            Plugin.RollManager?.ProcessIncomingRollMessage(
                $"Player{++_simPlayerIdx}@Bahamut", _rng.Next(1, outOf + 1), outOf);
        } else if (_state.Phase == TavernBrawlPhase.Rolling) {
            var pending = _state.Players.FirstOrDefault(p => !_state.CurrentRoundRolls.ContainsKey(p));
            if (pending != null)
                Plugin.RollManager?.ProcessIncomingRollMessage(pending, _rng.Next(1, outOf + 1), outOf);
        }
    }

    public override void ProcessRoll(Roll roll) {
        if (_state.Phase == TavernBrawlPhase.Registration) {
            TryJoin(roll.PlayerName, JoinSource.Roll);
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
                ["winner"] = PlayerName.Short(winner),
            });
            MatchHistory.Add(new TavernBrawlResult(winner, _state.Round, DateTime.Now));
            if (MatchHistory.Count > 10) MatchHistory.RemoveAt(0);
        }
    }

    public void ProcessChatMessage(string senderFullName, string message, XivChatType chatType) {
        if (!Cfg.AllowChatElimination) return;
        if (_state.Phase != TavernBrawlPhase.PendingChoice) return;
        if (!PlayerName.Short(senderFullName).Equals(PlayerName.Short(_state.HighestRoller ?? ""), StringComparison.OrdinalIgnoreCase)) return;

        var input = message.Trim();
        var match = _state.Players
            .Where(p => !p.Equals(_state.HighestRoller, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(p => PlayerName.Short(p).Equals(input, StringComparison.OrdinalIgnoreCase)
                              || p.Equals(input, StringComparison.OrdinalIgnoreCase));
        if (match == null) return;

        EliminateByChoice(match);
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }

}
