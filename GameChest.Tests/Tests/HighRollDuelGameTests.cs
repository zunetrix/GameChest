namespace GameChest.Tests;

public class HighRollDuelGameTests {
    private static (HighRollDuelGame game, HighRollDuelState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(configure);
        var game = new HighRollDuelGame(ctx);
        return (game, game.State);
    }

    [Fact]
    public void Phase_starts_idle() {
        var (_, state) = Create();
        state.Phase.ShouldBe(HighRollDuelPhase.Idle);
    }

    [Fact]
    public void BeginRegistration_sets_Registration_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        state.Phase.ShouldBe(HighRollDuelPhase.Registration);
    }

    [Fact]
    public void Players_register_during_registration() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 50, 100));
        state.Players.Count.ShouldBe(2);
    }

    [Fact]
    public void StartRolling_changes_phase_to_Rolling() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 50, 100));
        game.StartRolling();
        state.Phase.ShouldBe(HighRollDuelPhase.Rolling);
    }

    [Fact]
    public void Lowest_roller_eliminated_on_CloseRound() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 30, 100)); // lowest
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 60, 100));
        game.CloseRound();

        state.Players.ShouldNotContain("PlayerB@Bahamut");
        state.Players.Count.ShouldBe(2);
    }

    [Fact]
    public void Tied_lowest_rollers_all_eliminated() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 20, 100)); // tied lowest
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 20, 100)); // tied lowest
        game.CloseRound();

        state.Players.ShouldNotContain("PlayerB@Bahamut");
        state.Players.ShouldNotContain("PlayerC@Bahamut");
        state.Players.ShouldContain("PlayerA@Bahamut");
    }

    [Fact]
    public void Last_player_standing_wins_Done_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 20, 100));
        game.CloseRound();

        state.Phase.ShouldBe(HighRollDuelPhase.Done);
        state.Winner.ShouldBe("PlayerA@Bahamut");
    }

    [Fact]
    public void AutoCloseRound_fires_when_all_players_have_rolled() {
        var (game, state) = Create(c => c.HighRollDuel.AutoCloseRound = true);
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        // After second player rolls, AutoCloseRound triggers CloseRound automatically
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 20, 100));

        // Should have advanced past round 1 automatically
        state.Phase.ShouldBe(HighRollDuelPhase.Done);
        state.Winner.ShouldBe("PlayerA@Bahamut");
    }

    [Fact]
    public void AutoCloseRound_false_requires_manual_CloseRound() {
        // AutoCloseRound = false by default
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 20, 100));

        // Without CloseRound, phase stays Rolling
        state.Phase.ShouldBe(HighRollDuelPhase.Rolling);
        game.CloseRound();
        state.Phase.ShouldBe(HighRollDuelPhase.Done);
    }

    [Fact]
    public void Roll_with_wrong_OutOf_ignored_during_Rolling() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        // OutOf must be MaxRoll (100)
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 50)); // wrong OutOf
        state.CurrentRoundRolls.Count.ShouldBe(0);
    }
}
