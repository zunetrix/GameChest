using Dalamud.Game.Text;

namespace GameChest.Tests;

public class TavernBrawlGameTests {
    private static (TavernBrawlGame game, TavernBrawlState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(c => {
            c.TavernBrawl.MinPlayers = 2; // lower default (4) for tests
            configure?.Invoke(c);
        });
        var game = new TavernBrawlGame(ctx);
        return (game, game.State);
    }

    [Fact]
    public void Phase_starts_idle() {
        var (_, state) = Create();
        state.Phase.ShouldBe(TavernBrawlPhase.Idle);
    }

    [Fact]
    public void BeginRegistration_sets_Registration_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        state.Phase.ShouldBe(TavernBrawlPhase.Registration);
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
        state.Phase.ShouldBe(TavernBrawlPhase.Rolling);
    }

    [Fact]
    public void Lowest_roller_knocked_out_after_round_with_two_players() {
        // With 2 players: after lowest is knocked out, only 1 remains -> EndGame directly
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 30, 100)); // lowest

        // Round auto-closes; B eliminated; only A left -> Done
        state.Phase.ShouldBe(TavernBrawlPhase.Done);
        state.Winner.ShouldBe("PlayerA@Bahamut");
    }

    [Fact]
    public void Highest_roller_gets_PendingChoice_with_three_players() {
        var (game, state) = Create(c => c.TavernBrawl.MinPlayers = 2);
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        // A highest, B lowest, C middle
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 20, 100)); // lowest -> eliminated
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 50, 100));

        state.Phase.ShouldBe(TavernBrawlPhase.PendingChoice);
        state.HighestRoller.ShouldBe("PlayerA@Bahamut");
        state.LowestRoller.ShouldBe("PlayerB@Bahamut");
        state.Players.ShouldNotContain("PlayerB@Bahamut");
    }

    [Fact]
    public void EliminateByChoice_removes_target_and_advances_round() {
        var (game, state) = Create(c => c.TavernBrawl.MinPlayers = 2);
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 20, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 50, 100));
        // Phase = PendingChoice, A is highest

        game.EliminateByChoice("PlayerC@Bahamut");

        state.Players.ShouldNotContain("PlayerC@Bahamut");
        // Now only A remains -> Done
        state.Phase.ShouldBe(TavernBrawlPhase.Done);
        state.Winner.ShouldBe("PlayerA@Bahamut");
    }

    [Fact]
    public void Chat_elimination_works_when_allowed() {
        var (game, state) = Create(c => {
            c.TavernBrawl.MinPlayers = 2;
            c.TavernBrawl.AllowChatElimination = true;
        });
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 20, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 50, 100));
        // PendingChoice: A is highest

        // A sends short name of C in chat
        game.ProcessChatMessage("PlayerA@Bahamut", "PlayerC", XivChatType.Say);

        state.Players.ShouldNotContain("PlayerC@Bahamut");
    }

    [Fact]
    public void Chat_elimination_ignored_when_disabled() {
        var (game, state) = Create(c => {
            c.TavernBrawl.MinPlayers = 2;
            c.TavernBrawl.AllowChatElimination = false;
        });
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 1, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 1, 100));
        game.StartRolling();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 80, 100));
        game.ProcessRoll(new Roll("PlayerB@Bahamut", 20, 100));
        game.ProcessRoll(new Roll("PlayerC@Bahamut", 50, 100));
        // PendingChoice

        game.ProcessChatMessage("PlayerA@Bahamut", "PlayerC", XivChatType.Say);

        // C should still be in players (chat ignored)
        state.Players.ShouldContain("PlayerC@Bahamut");
        state.Phase.ShouldBe(TavernBrawlPhase.PendingChoice);
    }
}
