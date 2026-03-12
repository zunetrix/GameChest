using System;
using System.Collections.Generic;

using Dalamud.Game.Text;

using GameChest.Extensions;

namespace GameChest;

public record FightResult(string Winner, string Loser, int WinnerHp, DateTime PlayedAt);

public class FightGame : GameBase, IChatConsumer {
    public override string Name => "Fight Club";
    public override GameMode Mode => GameMode.FightGame;
    public override FightState State => _state;
    public override bool IsRegistering => _state.Phase == FightPhase.Registration;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => FightGamePhraseCategories.All;

    public List<FightResult> MatchHistory { get; } = new();

    private readonly FightState _state = new();
    private readonly Random _rng = new();
    private readonly Dictionary<string, DateTime> _outOfTurnCooldowns = new(StringComparer.OrdinalIgnoreCase);

    private Configuration.FightGameConfiguration Cfg => Plugin.Config.FightGame;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;
    public override void SetOutputChannel(XivChatType channel) => Cfg.OutputChannel = channel;

    internal FightGame(IPluginContext plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    public override void Start() {
        if (_state.RegisteredFighters.Count < 2) {
            DalamudApi.PluginLog.Warning("FightGame.Start: need 2 registered fighters.");
            return;
        }

        RollLog.Clear();
        var fighterA = _state.RegisteredFighters[0];
        var fighterB = _state.RegisteredFighters[1];

        _state.PlayerA = new FighterState(fighterA.FullName, Cfg.PlayerAHealth, Cfg.PlayerAMp);
        _state.PlayerB = new FighterState(fighterB.FullName, Cfg.PlayerBHealth, Cfg.PlayerBMp);
        _state.RegisteredFighters.Clear();
        _state.InitiativeRollA = null;
        _state.InitiativeRollB = null;
        _state.Phase = FightPhase.Initiative;
        _outOfTurnCooldowns.Clear();

        var vars = BuildVars(playerA: _state.PlayerA, playerB: _state.PlayerB, extra: new() {
            ["max"] = Cfg.MaxRollAllowed.ToString(),
        });
        PublishPhrase(FightGamePhraseCategories.FightStart, vars);
    }

    public override void Stop() {
        if (!_state.IsActive) return;

        var vars = new Dictionary<string, string> {
            ["playerA"] = _state.PlayerA != null ? PlayerName.Short(_state.PlayerA.FullName) : "",
            ["playerB"] = _state.PlayerB != null ? PlayerName.Short(_state.PlayerB.FullName) : "",
        };
        PublishPhrase(FightGamePhraseCategories.FightCanceled, vars);
        _state.Reset();
    }

    public override void RestartMatch() {
        if (_state.PlayerA == null || _state.PlayerB == null) return;
        var nameA = _state.PlayerA.FullName;
        var nameB = _state.PlayerB.FullName;

        if (_state.Phase == FightPhase.Finished && MatchHistory.Count > 0)
            MatchHistory.RemoveAtSafe(MatchHistory.Count - 1);

        RollLog.Clear();
        _state.Reset();
        _outOfTurnCooldowns.Clear();
        _state.RegisteredFighters.Add(new RegisteredFighter(nameA, RegistrationSource.Manual));
        _state.RegisteredFighters.Add(new RegisteredFighter(nameB, RegistrationSource.Manual));
        _state.Phase = FightPhase.Idle;
    }

    public override void Reset() {
        _state.Reset();
        _outOfTurnCooldowns.Clear();
    }

    public override void ProcessRoll(Roll roll) {
        switch (_state.Phase) {
            case FightPhase.Registration:
                if (Cfg.RegisterByRoll) TryRegister(roll.PlayerName, RegistrationSource.Roll);
                break;
            case FightPhase.Initiative:
                ProcessInitiative(roll);
                break;
            case FightPhase.Combat:
                ProcessCombat(roll);
                break;
        }
    }

    public void BeginRegistration() {
        _state.Reset();
        _state.Phase = FightPhase.Registration;
        _state.RegistrationReminderAt = DateTime.UtcNow.AddSeconds(Cfg.RegistrationReminderSeconds);

        var localPlayer = DalamudApi.PlayerState;
        var gmName = localPlayer.CharacterName;
        var vars = new Dictionary<string, string> {
            ["max"] = Cfg.MaxRollAllowed.ToString(),
            ["hp"] = Cfg.PlayerAHealth.ToString(),
            ["gm"] = gmName,
        };
        PublishPhrase(FightGamePhraseCategories.RegistrationStart, vars);
    }

    public void ProcessChatMessage(string senderFullName, string message, XivChatType chatType) {
        if (Cfg.RegisterByPhrase && message.Contains(Cfg.JoinGamePhrase, StringComparison.InvariantCultureIgnoreCase)) {
            DalamudApi.PluginLog.Debug($"{senderFullName} try join the game via phrase");
            TryRegister(senderFullName, RegistrationSource.Chat);
        }
    }

    public bool TryRegister(string fullName, RegistrationSource source) {
        var isOpenRegistration = _state.Phase == FightPhase.Registration;
        var isManualSource = source is RegistrationSource.Manual or RegistrationSource.Target;

        if (!isOpenRegistration && (!isManualSource || _state.Phase != FightPhase.Idle)) return false;
        if (_state.RegisteredFighters.Count >= 2) return false;
        if (_state.RegisteredFighters.Exists(f => PlayerName.Matches(f.FullName, fullName))) return false;

        // for manual entry
        if (Plugin.Config.IsBlockListActive && Plugin.Config.Blocklist.ContainsPlayer(fullName)) {
            Notification.ShowError($"{fullName} is on the blocklist and cannot be registered.");
            return false;
        }

        _state.RegisteredFighters.Add(new RegisteredFighter(fullName, source));
        var count = _state.RegisteredFighters.Count;

        if (isOpenRegistration) {
            var vars = new Dictionary<string, string> {
                ["player"] = PlayerName.Short(fullName),
                ["playerA"] = count >= 1 ? PlayerName.Short(_state.RegisteredFighters[0].FullName) : "",
                ["playerB"] = count >= 2 ? PlayerName.Short(_state.RegisteredFighters[1].FullName) : "",
            };
            PublishPhrase(FightGamePhraseCategories.RegistrationEntry, vars);

            if (count == 1) {
                PublishPhrase(FightGamePhraseCategories.RegistrationOneSlot, vars);
            } else {
                PublishPhrase(FightGamePhraseCategories.RegistrationFull, vars);
                _state.Phase = FightPhase.Idle;
            }
        }
        return true;
    }

    private void ProcessInitiative(Roll roll) {
        if (_state.PlayerA == null || _state.PlayerB == null) return;
        if (roll.OutOf != Cfg.MaxRollAllowed) {
            DalamudApi.PluginLog.Debug($"Initiative ignored: OutOf={roll.OutOf} expected={Cfg.MaxRollAllowed} player={roll.PlayerName}");
            return;
        }

        bool isA = PlayerName.Matches(roll.PlayerName, _state.PlayerA.FullName);
        bool isB = PlayerName.Matches(roll.PlayerName, _state.PlayerB.FullName);
        DalamudApi.PluginLog.Debug($"Initiative roll: player={roll.PlayerName} isA={isA}({_state.PlayerA.FullName}) isB={isB}({_state.PlayerB.FullName})");
        if (!isA && !isB) return;

        if (isA && _state.InitiativeRollA == null) {
            LogRoll(roll);
            _state.InitiativeRollA = roll.Result;
        }
        if (isB && _state.InitiativeRollB == null) {
            LogRoll(roll);
            _state.InitiativeRollB = roll.Result;
        }

        if (_state.InitiativeRollA == null || _state.InitiativeRollB == null) return;

        int rA = _state.InitiativeRollA.Value;
        int rB = _state.InitiativeRollB.Value;

        if (rA == rB) {
            _state.InitiativeRollA = null;
            _state.InitiativeRollB = null;
            var tieVars = new Dictionary<string, string> {
                ["rollA"] = rA.ToString(),
                ["rollB"] = rB.ToString(),
                ["playerA"] = PlayerName.Short(_state.PlayerA.FullName),
                ["playerB"] = PlayerName.Short(_state.PlayerB.FullName),
                ["max"] = Cfg.MaxRollAllowed.ToString(),
            };
            PublishPhrase(FightGamePhraseCategories.InitiativeTie, tieVars);
            return;
        }

        bool aWins = rA > rB;
        var attacker = aWins ? _state.PlayerA : _state.PlayerB;
        var defender = aWins ? _state.PlayerB : _state.PlayerA;
        _state.CurrentAttacker = attacker;
        _state.CurrentDefender = defender;
        _state.Phase = FightPhase.Combat;
        _state.TurnNumber = 1;
        _state.InactivityReminderAt = DateTime.UtcNow.AddSeconds(Cfg.InactivityReminderSeconds);

        var vars = new Dictionary<string, string> {
            ["attacker"] = PlayerName.Short(attacker.FullName),
            ["defender"] = PlayerName.Short(defender.FullName),
            ["rollA"] = rA.ToString(),
            ["rollB"] = rB.ToString(),
            ["playerA"] = PlayerName.Short(_state.PlayerA.FullName),
            ["playerB"] = PlayerName.Short(_state.PlayerB.FullName),
            ["max"] = Cfg.MaxRollAllowed.ToString(),
        };
        PublishPhrase(FightGamePhraseCategories.InitiativeResult, vars);
    }

    private void ProcessCombat(Roll roll) {
        if (_state.CurrentAttacker == null || _state.CurrentDefender == null) return;
        if (roll.OutOf != Cfg.MaxRollAllowed) return;

        if (!PlayerName.Matches(roll.PlayerName, _state.CurrentAttacker.FullName)) {
            SendOutOfTurnNotice(roll.PlayerName);
            return;
        }

        LogRoll(roll);
        var attacker = _state.CurrentAttacker;
        var defender = _state.CurrentDefender;
        int rollValue = roll.Result;
        int damage;
        string category;
        var extra = new Dictionary<string, string>();

        if (rollValue == 1) {
            attacker.SkipNextTurn = true;
            damage = 0;
            category = FightGamePhraseCategories.Fumble;
        } else if (rollValue == Cfg.MaxRollAllowed) {
            int bonus = _rng.Next(1, 11);
            damage = rollValue + bonus;
            category = FightGamePhraseCategories.CriticalHit;
            extra["bonus"] = bonus.ToString();
        } else {
            damage = rollValue;
            category = FightGamePhraseCategories.Attack;
        }

        if (damage > 0) {
            defender.Health = Math.Max(0, defender.Health - damage);
        }

        var vars = BuildCombatVars(attacker, defender, damage, extra);
        PublishPhrase(category, vars);

        if (defender.Health == 0) {
            EndFight(attacker, defender);
            return;
        }

        if (defender.Health <= (int)Math.Ceiling(defender.MaxHealth * 0.2f)) {
            var nearDeathVars = new Dictionary<string, string> {
                ["player"] = PlayerName.Short(defender.FullName),
                ["attacker"] = PlayerName.Short(attacker.FullName),
                ["defender"] = PlayerName.Short(defender.FullName),
                ["health"] = defender.Health.ToString(),
            };
            PublishPhrase(FightGamePhraseCategories.NearDeath, nearDeathVars);
        }

        AdvanceTurn();
    }

    private void AdvanceTurn() {
        _state.TurnNumber++;
        (_state.CurrentAttacker, _state.CurrentDefender) = (_state.CurrentDefender, _state.CurrentAttacker);

        if (_state.CurrentAttacker is { SkipNextTurn: true }) {
            _state.CurrentAttacker.SkipNextTurn = false;
            (_state.CurrentAttacker, _state.CurrentDefender) = (_state.CurrentDefender, _state.CurrentAttacker);
        }

        _state.InactivityReminderAt = DateTime.UtcNow.AddSeconds(Cfg.InactivityReminderSeconds);
    }

    private void EndFight(FighterState winner, FighterState loser) {
        var vars = new Dictionary<string, string> {
            ["winner"] = PlayerName.Short(winner.FullName),
            ["loser"] = PlayerName.Short(loser.FullName),
            ["attacker"] = PlayerName.Short(winner.FullName),
            ["defender"] = PlayerName.Short(loser.FullName),
            ["attackerHealth"] = winner.Health.ToString(),
            ["defenderHealth"] = loser.Health.ToString(),
            ["max"] = Cfg.MaxRollAllowed.ToString(),
        };
        PublishPhrase(FightGamePhraseCategories.FightEnd, vars);
        MatchHistory.Insert(0, new FightResult(PlayerName.Short(winner.FullName), PlayerName.Short(loser.FullName), winner.Health, DateTime.Now));
        if (MatchHistory.Count > 10) MatchHistory.RemoveAtSafe(MatchHistory.Count - 1);
        _state.Phase = FightPhase.Finished;
    }

    private void SendOutOfTurnNotice(string rollerName) {
        if (_state.CurrentAttacker == null) return;
        var now = DateTime.UtcNow;
        if (_outOfTurnCooldowns.TryGetValue(rollerName, out var last) &&
            (now - last).TotalSeconds < Cfg.OutOfTurnCooldownSeconds) return;

        _outOfTurnCooldowns[rollerName] = now;
        var display = PlayerName.Short(rollerName);
        var attackerDisplay = PlayerName.Short(_state.CurrentAttacker.FullName);
        Publish($"Not so fast, {display}! It's {attackerDisplay}'s turn!");
    }

    public override void Tick(DateTime now) {
        switch (_state.Phase) {
            case FightPhase.Registration:
                if (Cfg.RegistrationReminderSeconds > 0
                    && _state.RegistrationReminderAt.HasValue
                    && now >= _state.RegistrationReminderAt.Value) {
                    var vars = new Dictionary<string, string> { ["max"] = Cfg.MaxRollAllowed.ToString() };
                    PublishPhrase(FightGamePhraseCategories.RegistrationReminder, vars);
                    _state.RegistrationReminderAt = now.AddSeconds(Cfg.RegistrationReminderSeconds);
                }
                break;

            case FightPhase.Initiative:
            case FightPhase.Combat:
                if (Cfg.InactivityReminderSeconds > 0
                    && _state.InactivityReminderAt.HasValue
                    && now >= _state.InactivityReminderAt.Value
                    && _state.CurrentAttacker != null) {
                    var vars = new Dictionary<string, string> {
                        ["attacker"] = PlayerName.Short(_state.CurrentAttacker.FullName),
                        ["max"] = Cfg.MaxRollAllowed.ToString(),
                    };
                    PublishPhrase(FightGamePhraseCategories.InactivityReminder, vars);
                    _state.InactivityReminderAt = now.AddSeconds(Cfg.InactivityReminderSeconds);
                }
                break;
        }
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }

    private static Dictionary<string, string> BuildVars(
        FighterState? playerA = null,
        FighterState? playerB = null,
        Dictionary<string, string>? extra = null) {
        var vars = new Dictionary<string, string>();
        if (playerA != null) {
            vars["playerA"] = PlayerName.Short(playerA.FullName);
            vars["attackerHealth"] = playerA.Health.ToString();
        }
        if (playerB != null) {
            vars["playerB"] = PlayerName.Short(playerB.FullName);
            vars["defenderHealth"] = playerB.Health.ToString();
        }
        if (extra != null)
            foreach (var kv in extra) vars[kv.Key] = kv.Value;
        return vars;
    }

    private static Dictionary<string, string> BuildCombatVars(
        FighterState attacker, FighterState defender, int damage,
        Dictionary<string, string>? extra = null) {
        var vars = new Dictionary<string, string> {
            ["attacker"] = PlayerName.Short(attacker.FullName),
            ["defender"] = PlayerName.Short(defender.FullName),
            ["damage"] = damage.ToString(),
            ["health"] = defender.Health.ToString(),
            ["defenderHealth"] = defender.Health.ToString(),
            ["attackerHealth"] = attacker.Health.ToString(),
        };
        if (extra != null)
            foreach (var kv in extra) vars[kv.Key] = kv.Value;
        return vars;
    }


    public void SimulateRoll() {
        var outOf = Cfg.MaxRollAllowed;
        if (_state.Phase == FightPhase.Initiative) {
            if (_state.PlayerA != null && _state.InitiativeRollA == null)
                Plugin.RollManager?.ProcessIncomingRollMessage(_state.PlayerA.FullName, _rng.Next(1, outOf + 1), outOf);

            if (_state.PlayerB != null && _state.InitiativeRollB == null)
                Plugin.RollManager?.ProcessIncomingRollMessage(_state.PlayerB.FullName, _rng.Next(1, outOf + 1), outOf);

        } else if (_state.Phase == FightPhase.Combat && _state.CurrentAttacker != null) {
            Plugin.RollManager?.ProcessIncomingRollMessage(_state.CurrentAttacker.FullName, _rng.Next(1, outOf + 1), outOf);
        }
    }
}
