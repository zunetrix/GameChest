using System;
using System.Collections.Generic;

namespace GameChest;

public enum AssassinPhase { Idle, Registration, Active, Attacking, Done }

public record AssassinResult(string Winner, int PlayerCount, DateTime PlayedAt);

public sealed class AssassinGameState : IGameState {
    public AssassinPhase Phase { get; set; } = AssassinPhase.Idle;
    public bool IsActive => Phase is AssassinPhase.Registration or AssassinPhase.Active or AssassinPhase.Attacking;
    public List<string> Players { get; } = new();
    public Dictionary<string, string> Assignments { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string? CurrentAttacker { get; set; }
    public int? AttackRoll { get; set; }
    public string? CurrentDefender { get; set; }
    public int? DefenseRoll { get; set; }
    public string? Winner { get; set; }

    public void Reset() {
        Phase = AssassinPhase.Idle;
        Players.Clear();
        Assignments.Clear();
        CurrentAttacker = null;
        AttackRoll = null;
        CurrentDefender = null;
        DefenseRoll = null;
        Winner = null;
    }

    public void ResetAttack() {
        CurrentAttacker = null;
        AttackRoll = null;
        CurrentDefender = null;
        DefenseRoll = null;
    }
}
