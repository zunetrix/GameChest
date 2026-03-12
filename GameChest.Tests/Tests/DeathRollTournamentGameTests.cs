namespace GameChest.Tests;

public class DeathRollTournamentGameTests {
    private static (DeathRollTournamentGame game, DeathRollTournamentState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(configure);
        var game = new DeathRollTournamentGame(ctx);
        return (game, game.State);
    }

    [Fact]
    public void Phase_starts_idle() {
        var (_, state) = Create();
        state.Phase.ShouldBe(DeathRollTournamentPhase.Idle);
    }

    [Fact]
    public void BeginRegistration_sets_Registration_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        state.Phase.ShouldBe(DeathRollTournamentPhase.Registration);
    }

    [Fact]
    public void Players_register_during_Registration_with_roll_999() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 999));
        state.RegisteredPlayers.Count.ShouldBe(2);
    }

    [Fact]
    public void Registration_roll_with_non999_OutOf_is_rejected() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100)); // not 999
        state.RegisteredPlayers.Count.ShouldBe(0);
    }

    [Fact]
    public void CloseRegistration_creates_bracket_and_sets_Preparing() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 999));
        game.CloseRegistration();
        state.Phase.ShouldBe(DeathRollTournamentPhase.Preparing);
        state.Rounds.Count.ShouldBe(1);
    }

    [Fact]
    public void StartMatch_sets_Match_phase_with_two_players() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 999));
        game.CloseRegistration();
        game.StartMatch();
        state.Phase.ShouldBe(DeathRollTournamentPhase.Match);
        state.MatchPlayer1.ShouldNotBeNull();
        state.MatchPlayer2.ShouldNotBeNull();
    }

    [Fact]
    public void Lower_roll_of_1_loses_match() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 999));
        game.CloseRegistration();
        game.StartMatch();

        var p1 = state.MatchPlayer1!;
        var p2 = state.MatchPlayer2!;

        // p1 goes first with a mid value, p2 rolls 1 to lose
        game.ProcessRoll(new Roll(p1, 50, 999));
        game.ProcessRoll(new Roll(p2, 1, 50));

        state.MatchWinner.ShouldBe(p1);
    }

    [Fact]
    public void AdvanceToNextMatch_after_2player_bracket_ends_tournament() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 999));
        game.CloseRegistration();
        game.StartMatch();

        var p1 = state.MatchPlayer1!;
        var p2 = state.MatchPlayer2!;

        game.ProcessRoll(new Roll(p1, 50, 999));
        game.ProcessRoll(new Roll(p2, 1, 50));

        game.AdvanceToNextMatch();

        state.Phase.ShouldBe(DeathRollTournamentPhase.Done);
        state.TournamentWinner.ShouldBe(p1);
    }

    [Fact]
    public void Duplicate_registration_is_rejected() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 999));
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 999)); // duplicate
        state.RegisteredPlayers.Count.ShouldBe(1);
    }

    [Fact]
    public void ForfeitToPlayer_records_winner_and_advances() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 999));
        game.CloseRegistration();
        game.StartMatch();

        var p1 = state.MatchPlayer1!;
        game.ForfeitToPlayer(p1);

        state.Phase.ShouldBe(DeathRollTournamentPhase.Done);
        state.TournamentWinner.ShouldBe(p1);
    }
}
