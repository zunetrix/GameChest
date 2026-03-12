namespace GameChest.Tests;

public class KingOfTheHillGameTests {
    private static (KingOfTheHillGame game, KingOfTheHillState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(configure);
        var game = new KingOfTheHillGame(ctx);
        return (game, game.State);
    }

    [Fact]
    public void Phase_starts_idle() {
        var (_, state) = Create();
        state.Phase.ShouldBe(KingOfTheHillPhase.Idle);
    }

    [Fact]
    public void BeginRegistration_sets_Registration_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        state.Phase.ShouldBe(KingOfTheHillPhase.Registration);
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
        state.Phase.ShouldBe(KingOfTheHillPhase.Rolling);
    }

    [Fact]
    public void Highest_roll_takes_crown() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 40, 100));

        state.King.ShouldBe("PlayerA@Bahamut");
        state.KingHoldCount.ShouldBe(1);
    }

    [Fact]
    public void Different_player_wins_crown_if_they_roll_higher() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        // Round 1: A wins crown
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 40, 100));
        state.King.ShouldBe("PlayerA@Bahamut");

        // Round 2: B rolls higher -> new king
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 30, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 90, 100));
        state.King.ShouldBe("PlayerB@Bahamut");
        state.KingHoldCount.ShouldBe(1);
    }

    [Fact]
    public void King_hold_count_increments_when_defending() {
        var (game, state) = Create(c => c.KingOfTheHill.CrownHoldRounds = 5);
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        // A wins in rounds 1, 2, 3
        for (var i = 0; i < 3; i++) {
            game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
            game.ProcessRoll(new Roll("PlayerB@Bahamut", 40, 100));
        }

        state.King.ShouldBe("PlayerA@Bahamut");
        state.KingHoldCount.ShouldBe(3);
        state.Phase.ShouldBe(KingOfTheHillPhase.Rolling); // not done yet (need 5)
    }

    [Fact]
    public void Holding_crown_for_CrownHoldRounds_wins_game() {
        // Default CrownHoldRounds = 3
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        // A wins 3 rounds in a row
        for (var i = 0; i < 3; i++) {
            game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
            game.ProcessRoll(new Roll("PlayerB@Bahamut", 40, 100));
        }

        state.Phase.ShouldBe(KingOfTheHillPhase.Done);
        state.Winner.ShouldBe("PlayerA@Bahamut");
    }

    [Fact]
    public void Round_advances_when_all_players_rolled() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 40, 100));

        state.Round.ShouldBe(2); // advanced after round 1
    }

    [Fact]
    public void Stop_cancels_game() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();
        game.Stop();
        state.Phase.ShouldBe(KingOfTheHillPhase.Idle);
    }
}
