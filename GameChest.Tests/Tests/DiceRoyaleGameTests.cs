using Dalamud.Game.Text;

namespace GameChest.Tests;

public class DiceRoyaleGameTests {
    private static (DiceRoyaleGame game, DiceRoyaleState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(configure);
        var game = new DiceRoyaleGame(ctx);
        return (game, game.State);
    }

    [Fact]
    public void Phase_starts_idle() {
        var (_, state) = Create();
        state.Phase.ShouldBe(DiceRoyalePhase.Idle);
    }

    [Fact]
    public void BeginRegistration_sets_Registration_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        state.Phase.ShouldBe(DiceRoyalePhase.Registering);
    }

    [Fact]
    public void Players_register_during_Registration() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        state.Players.Count.ShouldBe(2);
    }

    [Fact]
    public void StartRolling_sets_Rolling_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();
        state.Phase.ShouldBe(DiceRoyalePhase.Rolling);
    }

    [Fact]
    public void Roll_1_to_20_eliminates_player() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 10, 100)); // 1-20: eliminated
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 50, 100)); // 21-60: survive
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 50, 100)); // 21-60: survive

        state.Players.ShouldNotContain("PlayerA@Bahamut");
        state.Players.ShouldContain("PlayerB@Bahamut");
        state.Players.ShouldContain("PlayerC@Bahamut");
    }

    [Fact]
    public void Roll_21_to_60_player_survives() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 30, 100)); // 21-60: survive
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 50, 100)); // 21-60: survive
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 50, 100)); // 21-60: survive

        // All survive -> next round
        state.Players.Count.ShouldBe(3);
        state.Phase.ShouldBe(DiceRoyalePhase.Rolling);
        state.Round.ShouldBe(2);
    }

    [Fact]
    public void Roll_91_to_100_creates_PendingElimination_with_multiple_targets() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 95, 100)); // 91-100: can eliminate
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 50, 100)); // survive
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 50, 100)); // survive

        // Round closes, A can eliminate someone, 2 targets available -> PendingElimination
        state.Phase.ShouldBe(DiceRoyalePhase.PendingElimination);
        state.CurrentEliminator.ShouldBe("PlayerA@Bahamut");
    }

    [Fact]
    public void EliminateByChoice_removes_target_and_advances() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 95, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 50, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 50, 100));

        game.EliminateByChoice("PlayerB@Bahamut");

        state.Players.ShouldNotContain("PlayerB@Bahamut");
        state.Phase.ShouldBe(DiceRoyalePhase.Rolling); // next round
    }

    [Fact]
    public void Last_player_wins() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        // A survives, B eliminated by low roll -> A wins
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 100)); // survive
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 10, 100)); // eliminated

        state.Phase.ShouldBe(DiceRoyalePhase.Finished);
        state.Winner.ShouldBe("PlayerA@Bahamut");
    }

    [Fact]
    public void Chat_elimination_works_when_allowed() {
        var (game, state) = Create(c => c.DiceRoyale.AllowChatElimination = true);
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 95, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 50, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 50, 100));

        game.ProcessChatMessage("PlayerA@Bahamut", "PlayerB", XivChatType.Say);
        state.Players.ShouldNotContain("PlayerB@Bahamut");
    }

    [Fact]
    public void Roll_61_to_90_advantage_player_stays() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 70, 100)); // 61-90: advantage, stays
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 50, 100)); // survive

        // Both stay, advance to round 2
        state.Players.Count.ShouldBe(2);
    }
}
