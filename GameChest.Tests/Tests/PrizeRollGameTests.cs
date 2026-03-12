namespace GameChest.Tests;

public class PrizeRollGameTests {
    private static (PrizeRollGame game, PrizeRollState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(configure);
        var game = new PrizeRollGame(ctx);
        return (game, game.State);
    }

    [Fact]
    public void Phase_starts_inactive() {
        var (_, state) = Create();
        state.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Start_sets_active() {
        var (game, state) = Create();
        game.Start();
        state.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Stop_ends_game() {
        var (game, state) = Create();
        game.Start();
        game.Stop();
        state.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Highest_roll_tracked_as_first_participant() {
        var (game, state) = Create();
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 80, 999));
        state.Participants.First!.RollResult.ShouldBe(80);
        state.Participants.First.FullName.ShouldBe("PlayerB@Bahamut");
    }

    [Fact]
    public void Lowest_roll_tracked_when_mode_is_lowest() {
        var (game, state) = Create(c => c.PrizeRoll.SortingMode = PrizeRollSortingMode.Lowest);
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 20, 999));
        state.Participants.First!.RollResult.ShouldBe(20);
        state.Participants.First.FullName.ShouldBe("PlayerB@Bahamut");
    }

    [Fact]
    public void Later_higher_roll_replaces_previous_best() {
        var (game, state) = Create();
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 80, 999));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 95, 999));
        state.Participants.First!.RollResult.ShouldBe(95);
        state.Participants.First.FullName.ShouldBe("PlayerC@Bahamut");
    }

    [Fact]
    public void Multiple_players_all_tracked() {
        var (game, state) = Create();
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 80, 999));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 30, 999));
        state.Participants.Entries.Count.ShouldBe(3);
    }

    [Fact]
    public void Wrong_OutOf_roll_is_ignored() {
        var (game, state) = Create();
        game.Start();
        // Default MaxRoll = 999, so OutOf = 100 should be rejected
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 100));
        state.Participants.Entries.Count.ShouldBe(0);
    }

    [Fact]
    public void Nearest_mode_tracks_closest_to_target() {
        var (game, state) = Create(c => {
            c.PrizeRoll.SortingMode = PrizeRollSortingMode.Nearest;
            c.PrizeRoll.NearestRoll = 500;
        });
        game.Start();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 300, 999)); // distance 200
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 510, 999)); // distance 10 → closest
        state.Participants.First!.FullName.ShouldBe("PlayerB@Bahamut");
    }

    [Fact]
    public void Rolls_not_accepted_when_inactive() {
        var (game, state) = Create();
        // Not started
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 50, 999));
        state.Participants.Entries.Count.ShouldBe(0);
    }
}
