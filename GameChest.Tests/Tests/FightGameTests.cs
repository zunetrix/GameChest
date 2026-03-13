namespace GameChest.Tests;

public class FightGameTests {
    private static (FightGame game, FightState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(configure);
        var game = new FightGame(ctx);
        return (game, game.State);
    }

    private static void RegisterTwoFighters(FightGame game, string nameA = "PlayerA@Bahamut",
        string nameB = "PlayerB@Bahamut") {
        game.BeginRegistration();
        game.ProcessRoll(new Roll(nameA, 5, 20)); // MaxRollAllowed = 20
        game.ProcessRoll(new Roll(nameB, 5, 20));
    }

    [Fact]
    public void Phase_starts_idle() {
        var (_, state) = Create();
        state.Phase.ShouldBe(FightPhase.Idle);
    }

    [Fact]
    public void BeginRegistration_sets_Registration_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        state.Phase.ShouldBe(FightPhase.Registering);
    }

    [Fact]
    public void Two_fighters_registered_via_roll() {
        var (game, state) = Create();
        RegisterTwoFighters(game);
        state.RegisteredFighters.Count.ShouldBe(2);
    }

    [Fact]
    public void Registration_closes_after_two_fighters() {
        var (game, state) = Create();
        RegisterTwoFighters(game);
        // After 2 fighters registered, phase becomes Idle (auto-closes registration)
        state.Phase.ShouldBe(FightPhase.Idle);
    }

    [Fact]
    public void Start_moves_to_Initiative_phase() {
        var (game, state) = Create();
        RegisterTwoFighters(game);
        game.Start();
        state.Phase.ShouldBe(FightPhase.Initiative);
    }

    [Fact]
    public void Start_sets_up_PlayerA_and_PlayerB() {
        var (game, state) = Create();
        RegisterTwoFighters(game, "Alice@Bahamut", "Bob@Bahamut");
        game.Start();
        state.PlayerA.ShouldNotBeNull();
        state.PlayerB.ShouldNotBeNull();
        state.PlayerA!.FullName.ShouldBe("Alice@Bahamut");
        state.PlayerB!.FullName.ShouldBe("Bob@Bahamut");
    }

    [Fact]
    public void Initiative_rolls_advance_to_Combat() {
        var (game, state) = Create();
        RegisterTwoFighters(game, "Alice@Bahamut", "Bob@Bahamut");
        game.Start();

        game.ProcessRoll(new Roll("Alice@Bahamut", 15, 20)); // A rolls higher
        game.ProcessRoll(new Roll("Bob@Bahamut", 8, 20));

        state.Phase.ShouldBe(FightPhase.Combat);
    }

    [Fact]
    public void Higher_initiative_roll_is_attacker() {
        var (game, state) = Create();
        RegisterTwoFighters(game, "Alice@Bahamut", "Bob@Bahamut");
        game.Start();

        game.ProcessRoll(new Roll("Alice@Bahamut", 18, 20)); // Alice rolls higher -> attacker
        game.ProcessRoll(new Roll("Bob@Bahamut", 5, 20));

        state.CurrentAttacker!.FullName.ShouldBe("Alice@Bahamut");
        state.CurrentDefender!.FullName.ShouldBe("Bob@Bahamut");
    }

    [Fact]
    public void Combat_roll_reduces_defender_health() {
        var (game, state) = Create();
        RegisterTwoFighters(game, "Alice@Bahamut", "Bob@Bahamut");
        game.Start();

        game.ProcessRoll(new Roll("Alice@Bahamut", 18, 20)); // Alice attacks first
        game.ProcessRoll(new Roll("Bob@Bahamut", 5, 20));

        var defender = state.CurrentDefender!;
        var initialHp = defender.Health;
        game.ProcessRoll(new Roll("Alice@Bahamut", 10, 20)); // 10 damage

        defender.Health.ShouldBe(initialHp - 10);
    }

    [Fact]
    public void Fighter_reaching_0_hp_ends_fight() {
        // Set low HP so we can end fight quickly
        var (game, state) = Create(c => {
            c.FightGame.PlayerAHealth = 5;
            c.FightGame.PlayerBHealth = 5;
        });
        RegisterTwoFighters(game, "Alice@Bahamut", "Bob@Bahamut");
        game.Start();

        game.ProcessRoll(new Roll("Alice@Bahamut", 18, 20)); // Alice attacks first
        game.ProcessRoll(new Roll("Bob@Bahamut", 5, 20));

        // Alice attacks with roll 5 (>= PlayerBHealth of 5) -> Bob hits 0 HP
        game.ProcessRoll(new Roll("Alice@Bahamut", 5, 20));

        state.Phase.ShouldBe(FightPhase.Finished);
    }

    [Fact]
    public void Tie_initiative_requires_reroll() {
        var (game, state) = Create();
        RegisterTwoFighters(game, "Alice@Bahamut", "Bob@Bahamut");
        game.Start();

        game.ProcessRoll(new Roll("Alice@Bahamut", 10, 20)); // tie
        game.ProcessRoll(new Roll("Bob@Bahamut", 10, 20));

        // Tie clears initiative, stays in Initiative phase
        state.Phase.ShouldBe(FightPhase.Initiative);
        state.InitiativeRollA.ShouldBeNull();
        state.InitiativeRollB.ShouldBeNull();
    }

    [Fact]
    public void Stop_cancels_game() {
        var (game, state) = Create();
        RegisterTwoFighters(game);
        game.Start();
        game.Stop();
        state.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Initiative_roll_with_wrong_OutOf_ignored() {
        var (game, state) = Create();
        RegisterTwoFighters(game, "Alice@Bahamut", "Bob@Bahamut");
        game.Start();

        game.ProcessRoll(new Roll("Alice@Bahamut", 10, 10)); // wrong OutOf (should be 20)
        state.InitiativeRollA.ShouldBeNull();
    }
}
