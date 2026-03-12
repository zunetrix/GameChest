namespace GameChest.Tests;

public class AssassinGameTests {
    private static (AssassinGame game, AssassinGameState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(configure);
        var game = new AssassinGame(ctx);
        return (game, game.State);
    }

    [Fact]
    public void Phase_starts_idle() {
        var (_, state) = Create();
        state.Phase.ShouldBe(AssassinPhase.Idle);
    }

    [Fact]
    public void BeginRegistration_sets_Registration_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        state.Phase.ShouldBe(AssassinPhase.Registration);
    }

    [Fact]
    public void Players_register_during_Registration() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 5, 20));
        state.Players.Count.ShouldBe(3);
    }

    [Fact]
    public void AssignTargets_creates_circular_assignment() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 5, 20));
        game.AssignTargets();

        state.Phase.ShouldBe(AssassinPhase.Active);
        state.Assignments.Count.ShouldBe(3);

        // Every player has exactly one target and is someone's target
        var allPlayers = new HashSet<string>(state.Players, StringComparer.OrdinalIgnoreCase);
        var allTargets = new HashSet<string>(state.Assignments.Values, StringComparer.OrdinalIgnoreCase);
        allTargets.SetEquals(allPlayers).ShouldBeTrue();
    }

    [Fact]
    public void AssignTargets_requires_min_players() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 5, 20));
        // Only 2 players, MinPlayers = 3 -> AssignTargets should not advance
        game.AssignTargets();
        state.Phase.ShouldBe(AssassinPhase.Registration);
    }

    [Fact]
    public void TriggerAttack_sets_Attacking_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 5, 20));
        game.AssignTargets();

        var attacker = state.Assignments.Keys.First();
        game.TriggerAttack(attacker);

        state.Phase.ShouldBe(AssassinPhase.Attacking);
        state.CurrentAttacker.ShouldBe(attacker);
        state.CurrentDefender.ShouldBe(state.Assignments[attacker]);
    }

    [Fact]
    public void Attacker_wins_when_rolls_higher() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 5, 20));
        game.AssignTargets();

        var attacker = state.Assignments.Keys.First();
        var defender = state.Assignments[attacker];
        game.TriggerAttack(attacker);

        game.ProcessRoll(new Roll(attacker, 18, 20));  // attacker rolls high
        game.ProcessRoll(new Roll(defender, 5, 20));   // defender rolls low

        state.Players.ShouldNotContain(defender);
        state.Phase.ShouldBe(AssassinPhase.Active);
    }

    [Fact]
    public void Defender_survives_when_rolls_equal_or_higher() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 5, 20));
        game.AssignTargets();

        var attacker = state.Assignments.Keys.First();
        var defender = state.Assignments[attacker];
        game.TriggerAttack(attacker);

        game.ProcessRoll(new Roll(attacker, 5, 20));   // attacker rolls low
        game.ProcessRoll(new Roll(defender, 18, 20));  // defender rolls high

        // Defender still in game
        state.Players.ShouldContain(defender);
        state.Phase.ShouldBe(AssassinPhase.Active);
    }

    [Fact]
    public void Last_player_wins() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 5, 20));
        game.AssignTargets();

        // Keep attacking until one player is left
        while (state.Players.Count > 1 && state.Phase == AssassinPhase.Active) {
            var attacker = state.Assignments.Keys.First();
            var defender = state.Assignments[attacker];
            game.TriggerAttack(attacker);
            game.ProcessRoll(new Roll(attacker, 20, 20)); // attacker always wins
            game.ProcessRoll(new Roll(defender, 1, 20));
        }

        state.Phase.ShouldBe(AssassinPhase.Done);
        state.Winner.ShouldNotBeNull();
    }

    [Fact]
    public void Target_transferred_after_successful_attack() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 5, 20));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 5, 20));
        game.AssignTargets();

        var attacker = state.Assignments.Keys.First();
        var defender = state.Assignments[attacker];
        var defenderTarget = state.Assignments[defender];

        game.TriggerAttack(attacker);
        game.ProcessRoll(new Roll(attacker, 18, 20));
        game.ProcessRoll(new Roll(defender, 5, 20));

        // Attacker now targets defender's former target
        state.Assignments[attacker].ShouldBe(defenderTarget);
    }
}
