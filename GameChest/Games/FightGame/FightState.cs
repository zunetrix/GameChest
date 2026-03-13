using System;
using System.Collections.Generic;

namespace GameChest;

public enum FightPhase {
    Idle,
    Registration,
    Initiative,
    Combat,
    Finished,
}

public sealed class RegisteredFighter {
    public RegisteredFighter(string fullName, JoinSource source) {
        FullName = fullName;
        Source = source;
    }

    public string FullName { get; }
    public JoinSource Source { get; }
}

public sealed class FighterState {
    public FighterState(string fullName, int maxHealth, int maxMp) {
        FullName = fullName;
        MaxHealth = maxHealth;
        Health = maxHealth;
        MaxMp = maxMp;
        Mp = maxMp;
    }

    public string FullName { get; }
    public int MaxHealth { get; }
    public int Health { get; set; }
    public int MaxMp { get; }
    public int Mp { get; set; }
    public bool SkipNextTurn { get; set; }
}

public sealed class FightState : IGameState {
    public FightPhase Phase { get; set; } = FightPhase.Idle;
    public FighterState? PlayerA { get; set; }
    public FighterState? PlayerB { get; set; }
    public FighterState? CurrentAttacker { get; set; }
    public FighterState? CurrentDefender { get; set; }
    public int? InitiativeRollA { get; set; }
    public int? InitiativeRollB { get; set; }
    public int TurnNumber { get; set; }
    public List<RegisteredFighter> RegisteredFighters { get; } = new();
    public DateTime? RegistrationReminderAt { get; set; }
    public DateTime? InactivityReminderAt { get; set; }

    public bool IsActive => Phase is FightPhase.Registration or FightPhase.Initiative or FightPhase.Combat;

    public void Reset() {
        Phase = FightPhase.Idle;
        PlayerA = null;
        PlayerB = null;
        CurrentAttacker = null;
        CurrentDefender = null;
        InitiativeRollA = null;
        InitiativeRollB = null;
        TurnNumber = 0;
        RegisteredFighters.Clear();
        RegistrationReminderAt = null;
        InactivityReminderAt = null;
    }
}
