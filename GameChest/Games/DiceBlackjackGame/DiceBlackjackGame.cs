using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;

namespace GameChest;

public class DiceBlackjackGame : GameBase, IChatConsumer {
    public override string Name => "Dice Blackjack";
    public override GameMode Mode => GameMode.DiceBlackjack;
    public override DiceBlackjackState State => _state;
    public override bool IsRegistering => _state.Phase == DiceBlackjackPhase.Registering;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => DiceBlackjackPhraseCategories.All;

    private readonly DiceBlackjackState _state = new();
    private readonly Random _rng = new();
    private int _simPlayerIdx;

    private Configuration.DiceBlackjackConfiguration Cfg => Plugin.Config.DiceBlackjack;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;
    public override void SetOutputChannel(XivChatType channel) => Cfg.OutputChannel = channel;

    internal DiceBlackjackGame(IPluginContext plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    public void BeginRegistration() {
        _state.Reset();
        _state.Phase = DiceBlackjackPhase.Registering;
        PublishPhrase(DiceBlackjackPhraseCategories.RegistrationOpen, new() { ["maxroll"] = Cfg.MaxRoll.ToString() });
    }

    public override void Start() => BeginRegistration();

    public override void Stop() {
        if (!_state.IsActive) return;
        _state.Phase = DiceBlackjackPhase.Idle;
        PublishPhrase(DiceBlackjackPhraseCategories.GameCanceled, new());
    }

    public void StartGame() {
        if (_state.Phase != DiceBlackjackPhase.Registering) return;
        if (_state.Players.Count < Cfg.MinPlayers) return;
        _state.Phase = DiceBlackjackPhase.PlayerTurns;
        _state.CurrentPlayerIndex = 0;
        AnnounceCurrentPlayer();
    }

    public override bool TryJoin(string fullName, JoinSource source) {
        if (_state.Phase != DiceBlackjackPhase.Registering) return false;
        if (_state.Players.Any(p => p.Name.Equals(fullName, StringComparison.OrdinalIgnoreCase))) return false;
        _state.Players.Add(new DiceBlackjackPlayerHand(fullName));
        return true;
    }

    public void Stand() {
        if (_state.Phase != DiceBlackjackPhase.PlayerTurns) return;
        var current = _state.CurrentPlayer;
        if (current == null || current.DealCount < 2) return;
        StandCurrentPlayer();
    }

    public void DealerStand() {
        if (_state.Phase != DiceBlackjackPhase.DealerTurn) return;
        if (_state.DealerStatus != PlayerHandStatus.Active) return;
        var total = HandTotal(_state.DealerCards);
        _state.DealerStatus = PlayerHandStatus.Standing;
        PublishPhrase(DiceBlackjackPhraseCategories.DealerStand, new() { ["total"] = total.ToString() });
        EndGame();
    }

    public override void ProcessRoll(Roll roll) {
        if (roll.OutOf != Cfg.MaxRoll) return;

        if (_state.Phase == DiceBlackjackPhase.Registering) {
            TryJoin(roll.PlayerName, JoinSource.Roll);
            return;
        }

        if (_state.Phase == DiceBlackjackPhase.DealerTurn) {
            if (_state.DealerStatus == PlayerHandStatus.Active)
                AddDealerCard(roll.Result);
            return;
        }

        if (_state.Phase != DiceBlackjackPhase.PlayerTurns) return;
        var current = _state.CurrentPlayer;
        if (current == null) return;
        if (!current.Name.Equals(roll.PlayerName, StringComparison.OrdinalIgnoreCase)) return;

        current.Cards.Add(roll.Result);
        var total = HandTotal(current.Cards);

        if (current.DealCount < 2) {
            current.DealCount++;
            PublishPhrase(DiceBlackjackPhraseCategories.PlayerDealt, new() {
                ["player"] = PlayerName.Short(current.Name),
                ["card"]   = CardLabel(roll.Result),
                ["total"]  = total.ToString(),
            });
            if (current.DealCount >= 2 && total > Cfg.TargetPoints)
                BustCurrentPlayer();
        } else {
            PublishPhrase(DiceBlackjackPhraseCategories.PlayerHit, new() {
                ["player"] = PlayerName.Short(current.Name),
                ["card"]   = CardLabel(roll.Result),
                ["total"]  = total.ToString(),
            });
            if (total > Cfg.TargetPoints)
                BustCurrentPlayer();
        }
    }

    public void ProcessChatMessage(string senderFullName, string message, XivChatType chatType) {
        if (_state.Phase != DiceBlackjackPhase.PlayerTurns) return;
        var current = _state.CurrentPlayer;
        if (current == null || current.DealCount < 2) return;
        if (!PlayerName.Short(senderFullName).Equals(PlayerName.Short(current.Name), StringComparison.OrdinalIgnoreCase)) return;
        if (message.Trim().Equals("stand", StringComparison.OrdinalIgnoreCase))
            StandCurrentPlayer();
    }

    public void SimulateRoll() {
        var outOf = Cfg.MaxRoll;
        if (_state.Phase == DiceBlackjackPhase.Registering) {
            Plugin.RollManager?.ProcessIncomingRollMessage($"Player{++_simPlayerIdx}@Bahamut", _rng.Next(1, outOf + 1), outOf);
        } else if (_state.Phase == DiceBlackjackPhase.PlayerTurns) {
            var current = _state.CurrentPlayer;
            if (current != null)
                Plugin.RollManager?.ProcessIncomingRollMessage(current.Name, _rng.Next(1, outOf + 1), outOf);
        } else if (_state.Phase == DiceBlackjackPhase.DealerTurn) {
            Plugin.RollManager?.ProcessIncomingRollMessage("Dealer@Bahamut", _rng.Next(1, outOf + 1), outOf);
        }
    }

    private void StandCurrentPlayer() {
        var current = _state.CurrentPlayer;
        if (current == null) return;
        current.Status = PlayerHandStatus.Standing;
        PublishPhrase(DiceBlackjackPhraseCategories.PlayerStand, new() {
            ["player"] = PlayerName.Short(current.Name),
            ["total"]  = HandTotal(current.Cards).ToString(),
        });
        AdvanceToNextPlayer();
    }

    private void BustCurrentPlayer() {
        var current = _state.CurrentPlayer;
        if (current == null) return;
        current.Status = PlayerHandStatus.Busted;
        PublishPhrase(DiceBlackjackPhraseCategories.PlayerBust, new() {
            ["player"] = PlayerName.Short(current.Name),
            ["total"]  = HandTotal(current.Cards).ToString(),
        });
        AdvanceToNextPlayer();
    }

    private void AdvanceToNextPlayer() {
        _state.CurrentPlayerIndex++;
        while (_state.CurrentPlayerIndex < _state.Players.Count
               && _state.Players[_state.CurrentPlayerIndex].Status != PlayerHandStatus.Active)
            _state.CurrentPlayerIndex++;

        if (_state.CurrentPlayerIndex >= _state.Players.Count) {
            if (_state.Players.All(p => p.Status == PlayerHandStatus.Busted)) {
                _state.Phase = DiceBlackjackPhase.Finished;
            } else {
                _state.Phase = DiceBlackjackPhase.DealerTurn;
            }
        } else {
            AnnounceCurrentPlayer();
        }
    }

    private void AddDealerCard(int roll) {
        _state.DealerCards.Add(roll);
        var total = HandTotal(_state.DealerCards);
        PublishPhrase(DiceBlackjackPhraseCategories.DealerDraw, new() {
            ["card"]  = CardLabel(roll),
            ["total"] = total.ToString(),
        });

        if (total > Cfg.TargetPoints) {
            _state.DealerStatus = PlayerHandStatus.Busted;
            PublishPhrase(DiceBlackjackPhraseCategories.DealerBust, new() { ["total"] = total.ToString() });
            EndGame();
        } else if (total >= Cfg.DealerStandAt) {
            _state.DealerStatus = PlayerHandStatus.Standing;
            PublishPhrase(DiceBlackjackPhraseCategories.DealerStand, new() { ["total"] = total.ToString() });
            EndGame();
        }
    }

    private void EndGame() {
        _state.Phase = DiceBlackjackPhase.Finished;
        var dealerTotal = HandTotal(_state.DealerCards);
        var dealerBusted = _state.DealerStatus == PlayerHandStatus.Busted;
        string? champion = null;
        int championScore = 0;

        foreach (var p in _state.Players) {
            if (p.Status == PlayerHandStatus.Busted) continue;
            var score = HandTotal(p.Cards);
            if (dealerBusted || score > dealerTotal) {
                PublishPhrase(DiceBlackjackPhraseCategories.PlayerWin, new() {
                    ["player"] = PlayerName.Short(p.Name),
                    ["score"]  = score.ToString(),
                });
                if (champion == null || score > championScore) {
                    champion = p.Name;
                    championScore = score;
                }
            } else if (score == dealerTotal) {
                PublishPhrase(DiceBlackjackPhraseCategories.PlayerPush, new() {
                    ["player"] = PlayerName.Short(p.Name),
                    ["score"]  = score.ToString(),
                });
            } else {
                PublishPhrase(DiceBlackjackPhraseCategories.PlayerLoss, new() {
                    ["player"] = PlayerName.Short(p.Name),
                    ["score"]  = score.ToString(),
                });
            }
        }

        if (champion != null) {
            _state.Winner = champion;
            PublishPhrase(DiceBlackjackPhraseCategories.GameEnd, new() {
                ["winner"] = PlayerName.Short(champion),
                ["score"]  = championScore.ToString(),
            });
        }
    }

    private void AnnounceCurrentPlayer() {
        var current = _state.CurrentPlayer;
        if (current == null) return;
        PublishPhrase(DiceBlackjackPhraseCategories.PlayerTurn, new() {
            ["player"]  = PlayerName.Short(current.Name),
            ["maxroll"] = Cfg.MaxRoll.ToString(),
        });
    }

    public int HandTotal(IEnumerable<int> cards) {
        int total = 0, aces = 0;
        foreach (var roll in cards) {
            if (Cfg.CardMapping && Cfg.MaxRoll >= 11 && roll >= 11) { total += 10; continue; }
            if (Cfg.CardMapping && roll == 1) { aces++; total += 11; continue; }
            total += roll;
        }
        while (total > Cfg.TargetPoints && aces > 0) { total -= 10; aces--; }
        return total;
    }

    public string CardLabel(int roll) {
        if (!Cfg.CardMapping || Cfg.MaxRoll < 11) return roll.ToString();
        return roll switch { 1 => "A", 11 => "J", 12 => "Q", 13 => "K", _ => roll.ToString() };
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }

}
