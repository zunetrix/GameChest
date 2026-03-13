namespace GameChest.Tests;

public class DeathRollGameTests {
    private static (DeathRollGame game, DeathRollState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(configure);
        var game = new DeathRollGame(ctx);
        return (game, game.State);
    }

    [Fact]
    public void Phase_starts_idle() {
        var (_, state) = Create();
        state.Phase.ShouldBe(DeathRollPhase.Idle);
    }

    [Fact]
    public void Start_sets_Active() {
        var (game, state) = Create();
        game.Start();
        state.Phase.ShouldBe(DeathRollPhase.Active);
    }

    [Fact]
    public void First_roll_sets_chain_value() {
        var (game, state) = Create();
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        state.Chain.Count.ShouldBe(1);
        state.Chain[0].Result.ShouldBe(50);
    }

    [Fact]
    public void Subsequent_roll_reduces_chain() {
        var (game, state) = Create();
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 30, 50));
        state.Chain.Count.ShouldBe(2);
        state.Chain[1].Result.ShouldBe(30);
    }

    [Fact]
    public void Wrong_OutOf_for_subsequent_roll_is_rejected() {
        var (game, state) = Create();
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        // Correct OutOf would be 50 (last result), sending 999 should be rejected
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 20, 999));
        state.Chain.Count.ShouldBe(1);
    }

    [Fact]
    public void First_roll_with_wrong_OutOf_is_rejected() {
        var (game, state) = Create();
        game.Start();
        // Default StartingRoll = 999, sending 100 should be rejected
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 100));
        state.Chain.Count.ShouldBe(0);
    }

    [Fact]
    public void Rolling_1_ends_game_with_loser() {
        var (game, state) = Create();
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 50));
        state.Phase.ShouldBe(DeathRollPhase.Finished);
        state.Loser.ShouldBe("PlayerB@Bahamut");
        state.Winner.ShouldBe("PlayerA@Bahamut");
    }

    [Fact]
    public void Rolling_1_on_first_roll_ends_game() {
        var (game, state) = Create();
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 999));
        state.Phase.ShouldBe(DeathRollPhase.Finished);
        state.Loser.ShouldBe("PlayerA@Bahamut");
        // No winner since only 1 roll in chain
        state.Winner.ShouldBeNull();
    }

    [Fact]
    public void Players_alternate_turns_tracked_in_chain() {
        var (game, state) = Create();
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 30, 50));
        state.Chain[0].PlayerName.ShouldBe("PlayerA@Bahamut");
        state.Chain[1].PlayerName.ShouldBe("PlayerB@Bahamut");
    }

    [Fact]
    public void Same_player_cannot_roll_twice_in_a_row() {
        var (game, state) = Create();
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 30, 50)); // same player - ignored
        state.Chain.Count.ShouldBe(1);
    }

    [Fact]
    public void Custom_starting_roll_is_respected() {
        var (game, state) = Create(c => c.DeathRoll.StartingRoll = 100);
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999)); // wrong outOf for 100
        state.Chain.Count.ShouldBe(0);
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 100)); // correct
        state.Chain.Count.ShouldBe(1);
    }
}
