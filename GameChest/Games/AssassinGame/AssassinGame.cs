using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;

namespace GameChest;

public class AssassinGame : GameBase {
    public override string Name => "Assassin";
    public override GameMode Mode => GameMode.AssassinGame;
    public override AssassinGameState State => _state;
    public override bool IsRegistering => _state.Phase == AssassinPhase.Registration;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => AssassinGamePhraseCategories.All;

    public List<AssassinResult> MatchHistory { get; } = new();

    private readonly AssassinGameState _state = new();
    private readonly Random _rng = new();
    private int _simPlayerIdx;
    private Configuration.AssassinGameConfiguration Cfg => Plugin.Config.AssassinGame;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;
    public override void SetOutputChannel(XivChatType channel) => Cfg.OutputChannel = channel;

    internal AssassinGame(IPluginContext plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    public void BeginRegistration() {
        _state.Reset();
        _state.Phase = AssassinPhase.Registration;
        PublishPhrase(AssassinGamePhraseCategories.RegistrationOpen, new Dictionary<string, string>());
    }

    public override void Start() => BeginRegistration();

    public void AssignTargets() {
        if (_state.Players.Count < Cfg.MinPlayers) return;
        _state.Assignments.Clear();
        var shuffled = _state.Players.OrderBy(_ => _rng.Next()).ToList();
        for (var i = 0; i < shuffled.Count; i++)
            _state.Assignments[shuffled[i]] = shuffled[(i + 1) % shuffled.Count];
        _state.Phase = AssassinPhase.Active;
        PublishPhrase(AssassinGamePhraseCategories.GameStart, new Dictionary<string, string>());
    }

    public override void Stop() {
        if (!_state.IsActive) return;
        _state.Phase = AssassinPhase.Idle;
        PublishPhrase(AssassinGamePhraseCategories.GameCanceled, new Dictionary<string, string>());
    }

    public bool TryRegister(string fullName) {
        if (_state.Phase != AssassinPhase.Registration) return false;
        if (_state.Players.Contains(fullName, StringComparer.OrdinalIgnoreCase)) return false;
        _state.Players.Add(fullName);
        return true;
    }

    /// <summary>GM triggers an attack by a specific player against their assigned target.</summary>
    public void TriggerAttack(string attackerName) {
        if (_state.Phase != AssassinPhase.Active) return;
        if (!_state.Assignments.TryGetValue(attackerName, out var defender)) return;
        _state.CurrentAttacker = attackerName;
        _state.CurrentDefender = defender;
        _state.AttackRoll = null;
        _state.DefenseRoll = null;
        _state.Phase = AssassinPhase.Attacking;
        PublishPhrase(AssassinGamePhraseCategories.AttackAttempt, new Dictionary<string, string> {
            ["attacker"] = PlayerName.Short(attackerName),
            ["defender"] = PlayerName.Short(defender),
            ["maxroll"]  = Cfg.MaxRoll.ToString(),
        });
    }

    public void SimulateRoll() {
        var outOf = Cfg.MaxRoll;
        if (_state.Phase == AssassinPhase.Registration) {
            Plugin.RollManager?.ProcessIncomingRollMessage(
                $"Player{++_simPlayerIdx}@Bahamut", _rng.Next(1, outOf + 1), outOf);
        } else if (_state.Phase == AssassinPhase.Attacking) {
            if (_state.AttackRoll == null && _state.CurrentAttacker != null)
                Plugin.RollManager?.ProcessIncomingRollMessage(
                    _state.CurrentAttacker, _rng.Next(1, outOf + 1), outOf);
            if (_state.DefenseRoll == null && _state.CurrentDefender != null)
                Plugin.RollManager?.ProcessIncomingRollMessage(
                    _state.CurrentDefender, _rng.Next(1, outOf + 1), outOf);
        }
    }

    public override void ProcessRoll(Roll roll) {
        if (_state.Phase == AssassinPhase.Registration) { TryRegister(roll.PlayerName); return; }
        if (_state.Phase != AssassinPhase.Attacking) return;
        if (roll.OutOf != Cfg.MaxRoll) return;

        var isAttacker = PlayerName.Short(roll.PlayerName).Equals(PlayerName.Short(_state.CurrentAttacker ?? ""), StringComparison.OrdinalIgnoreCase);
        var isDefender = PlayerName.Short(roll.PlayerName).Equals(PlayerName.Short(_state.CurrentDefender ?? ""), StringComparison.OrdinalIgnoreCase);

        if (isAttacker && _state.AttackRoll == null) {
            _state.AttackRoll = roll.Result;
        } else if (isDefender && _state.DefenseRoll == null) {
            _state.DefenseRoll = roll.Result;
        }

        if (_state.AttackRoll.HasValue && _state.DefenseRoll.HasValue)
            ResolveAttack();
    }

    private void ResolveAttack() {
        var aRoll = _state.AttackRoll!.Value;
        var dRoll = _state.DefenseRoll!.Value;
        var attacker = _state.CurrentAttacker!;
        var defender = _state.CurrentDefender!;

        if (aRoll > dRoll) {
            // Success
            PublishPhrase(AssassinGamePhraseCategories.AssassinationSuccess, new Dictionary<string, string> {
                ["attacker"] = PlayerName.Short(attacker), ["aroll"] = aRoll.ToString(),
                ["defender"] = PlayerName.Short(defender), ["droll"] = dRoll.ToString(),
            });
            // Transfer target
            if (_state.Assignments.TryGetValue(defender, out var nextTarget))
                _state.Assignments[attacker] = nextTarget;
            _state.Assignments.Remove(defender);
            _state.Players.Remove(defender);

            PublishPhrase(AssassinGamePhraseCategories.PlayerEliminated, new Dictionary<string, string> {
                ["player"] = PlayerName.Short(defender),
                ["remaining"] = _state.Players.Count.ToString(),
            });

            if (_state.Players.Count <= 1) { EndGame(); return; }
        } else {
            PublishPhrase(AssassinGamePhraseCategories.AssassinationFailed, new Dictionary<string, string> {
                ["attacker"] = PlayerName.Short(attacker), ["aroll"] = aRoll.ToString(),
                ["defender"] = PlayerName.Short(defender), ["droll"] = dRoll.ToString(),
            });
        }

        _state.ResetAttack();
        _state.Phase = AssassinPhase.Active;
    }

    private void EndGame() {
        var winner = _state.Players.FirstOrDefault();
        _state.Winner = winner;
        _state.Phase = AssassinPhase.Done;
        if (winner != null) {
            PublishPhrase(AssassinGamePhraseCategories.GameEnd, new Dictionary<string, string> { ["winner"] = PlayerName.Short(winner) });
            MatchHistory.Add(new AssassinResult(winner, _state.Players.Count, DateTime.Now));
            if (MatchHistory.Count > 10) MatchHistory.RemoveAt(0);
        }
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }

}
