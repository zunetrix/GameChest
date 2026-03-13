using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;

namespace GameChest;

public class DiceRoyaleGame : GameBase, IChatConsumer {
    public override string Name => "Dice Royale";
    public override GameMode Mode => GameMode.DiceRoyale;
    public override DiceRoyaleState State => _state;
    public override bool IsRegistering => _state.Phase == DiceRoyalePhase.Registering;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => DiceRoyalePhraseCategories.All;

    public List<DiceRoyaleResult> MatchHistory { get; } = new();

    private readonly DiceRoyaleState _state = new();
    private readonly Random _rng = new();
    private int _simPlayerIdx;
    private Configuration.DiceRoyaleConfiguration Cfg => Plugin.Config.DiceRoyale;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;
    public override void SetOutputChannel(XivChatType channel) => Cfg.OutputChannel = channel;

    internal DiceRoyaleGame(IPluginContext plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    public void BeginRegistration() {
        _state.Reset();
        _state.Phase = DiceRoyalePhase.Registering;
        PublishPhrase(DiceRoyalePhraseCategories.RegistrationOpen, new Dictionary<string, string>());
    }

    public override void Start() => BeginRegistration();

    public void StartRolling() {
        if (_state.Players.Count < Cfg.MinPlayers) return;
        _state.Phase = DiceRoyalePhase.Rolling;
        _state.Round = 1;
        AnnounceRoundStart();
    }

    public override void Stop() {
        if (!_state.IsActive) return;
        _state.Phase = DiceRoyalePhase.Idle;
        PublishPhrase(DiceRoyalePhraseCategories.GameCanceled, new Dictionary<string, string>());
    }

    public override bool TryJoin(string fullName, JoinSource source) {
        if (_state.Phase != DiceRoyalePhase.Registering &&
            (_state.Phase != DiceRoyalePhase.Idle || source is not (JoinSource.Manual or JoinSource.Target)))
            return false;
        if (_state.Players.Contains(fullName, StringComparer.OrdinalIgnoreCase)) return false;
        _state.Players.Add(fullName);
        return true;
    }

    public void CloseRound() {
        if (_state.Phase != DiceRoyalePhase.Rolling) return;
        if (_state.CurrentRoundRolls.Count == 0) return;

        var eliminators = new List<string>();

        foreach (var (player, roll) in _state.CurrentRoundRolls) {
            if (roll <= 20) {
                _state.Players.Remove(player);
                PublishPhrase(DiceRoyalePhraseCategories.PlayerEliminated, new Dictionary<string, string> {
                    ["player"] = PlayerName.Short(player), ["roll"] = roll.ToString(),
                });
            } else if (roll <= 60) {
                PublishPhrase(DiceRoyalePhraseCategories.PlayerSurvives, new Dictionary<string, string> {
                    ["player"] = PlayerName.Short(player), ["roll"] = roll.ToString(),
                });
            } else if (roll <= 90) {
                PublishPhrase(DiceRoyalePhraseCategories.PlayerAdvantage, new Dictionary<string, string> {
                    ["player"] = PlayerName.Short(player), ["roll"] = roll.ToString(),
                });
            } else {
                eliminators.Add(player);
                PublishPhrase(DiceRoyalePhraseCategories.PlayerEliminates, new Dictionary<string, string> {
                    ["player"] = PlayerName.Short(player), ["roll"] = roll.ToString(),
                });
            }
        }

        if (_state.Players.Count <= 1) { EndGame(); return; }

        // Queue eliminators who are still alive
        foreach (var e in eliminators.Where(e => _state.Players.Contains(e, StringComparer.OrdinalIgnoreCase)))
            _state.PendingEliminators.Enqueue(e);

        if (_state.PendingEliminators.Count > 0) {
            AdvanceEliminator();
        } else {
            NextRound();
        }
    }

    /// <summary>GM selects the target for the current eliminator.</summary>
    public void EliminateByChoice(string targetName) {
        if (_state.Phase != DiceRoyalePhase.PendingElimination) return;
        _state.Players.Remove(targetName);
        PublishPhrase(DiceRoyalePhraseCategories.PlayerEliminated, new Dictionary<string, string> {
            ["player"] = PlayerName.Short(targetName), ["roll"] = "eliminated",
        });

        if (_state.Players.Count <= 1) { EndGame(); return; }

        if (_state.PendingEliminators.Count > 0) {
            AdvanceEliminator();
        } else {
            NextRound();
        }
    }

    public void ProcessChatMessage(string senderFullName, string message, XivChatType chatType) {
        if (!Cfg.AllowChatElimination) return;
        if (_state.Phase != DiceRoyalePhase.PendingElimination) return;
        if (!PlayerName.Short(senderFullName).Equals(PlayerName.Short(_state.CurrentEliminator ?? ""), StringComparison.OrdinalIgnoreCase)) return;

        var input = message.Trim();
        var match = _state.Players
            .Where(p => !p.Equals(_state.CurrentEliminator, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(p => PlayerName.Short(p).Equals(input, StringComparison.OrdinalIgnoreCase)
                              || p.Equals(input, StringComparison.OrdinalIgnoreCase));

        if (match == null) return;
        EliminateByChoice(match);
    }

    public void SimulateRoll() {
        var outOf = Cfg.MaxRoll;
        if (_state.Phase == DiceRoyalePhase.Registering) {
            Plugin.RollManager?.ProcessIncomingRollMessage(
                $"Player{++_simPlayerIdx}@Bahamut", _rng.Next(1, outOf + 1), outOf);
        } else if (_state.Phase == DiceRoyalePhase.Rolling) {
            var pending = _state.Players.FirstOrDefault(p => !_state.CurrentRoundRolls.ContainsKey(p));
            if (pending != null)
                Plugin.RollManager?.ProcessIncomingRollMessage(pending, _rng.Next(1, outOf + 1), outOf);
        }
    }

    public override void ProcessRoll(Roll roll) {
        if (_state.Phase == DiceRoyalePhase.Registering) { TryJoin(roll.PlayerName, JoinSource.Roll); return; }
        if (_state.Phase != DiceRoyalePhase.Rolling) return;
        if (roll.OutOf != Cfg.MaxRoll) return;
        if (!_state.Players.Contains(roll.PlayerName, StringComparer.OrdinalIgnoreCase)) return;
        if (_state.CurrentRoundRolls.ContainsKey(roll.PlayerName)) return;

        _state.CurrentRoundRolls[roll.PlayerName] = roll.Result;

        if (_state.CurrentRoundRolls.Count >= _state.Players.Count)
            CloseRound();
    }

    private void NextRound() {
        _state.Round++;
        _state.ResetRound();
        _state.Phase = DiceRoyalePhase.Rolling;
        AnnounceRoundStart();
    }

    /// <summary>Dequeues the next eliminator; auto-eliminates if only one target remains.</summary>
    private void AdvanceEliminator() {
        _state.CurrentEliminator = _state.PendingEliminators.Dequeue();
        _state.Phase = DiceRoyalePhase.PendingElimination;

        var eligible = _state.Players
            .Where(p => !p.Equals(_state.CurrentEliminator, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (eligible.Count == 1)
            EliminateByChoice(eligible[0]);
    }

    private void AnnounceRoundStart() {
        PublishPhrase(DiceRoyalePhraseCategories.RoundStart, new Dictionary<string, string> {
            ["round"] = _state.Round.ToString(), ["maxroll"] = Cfg.MaxRoll.ToString(),
        });
    }

    private void EndGame() {
        var winner = _state.Players.FirstOrDefault();
        _state.Winner = winner;
        _state.Phase = DiceRoyalePhase.Finished;
        if (winner != null) {
            PublishPhrase(DiceRoyalePhraseCategories.GameEnd, new Dictionary<string, string> { ["winner"] = PlayerName.Short(winner) });
            MatchHistory.Add(new DiceRoyaleResult(winner, _state.Round, DateTime.Now));
            if (MatchHistory.Count > 10) MatchHistory.RemoveAt(0);
        }
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }

}
