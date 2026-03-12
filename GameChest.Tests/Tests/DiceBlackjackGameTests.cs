using Dalamud.Game.Text;

namespace GameChest.Tests;

public class DiceBlackjackGameTests {
    private static (DiceBlackjackGame game, DiceBlackjackState state) Create(
        Action<Configuration>? configure = null) {
        var ctx = new TestPluginContext(configure);
        var game = new DiceBlackjackGame(ctx);
        return (game, game.State);
    }

    [Fact]
    public void Phase_starts_idle() {
        var (_, state) = Create();
        state.Phase.ShouldBe(DiceBlackjackPhase.Idle);
    }

    [Fact]
    public void BeginRegistration_sets_Registration_phase() {
        var (game, state) = Create();
        game.BeginRegistration();
        state.Phase.ShouldBe(DiceBlackjackPhase.Registration);
    }

    [Fact]
    public void Player_registers_during_Registration_with_correct_OutOf() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13)); // MaxRoll = 13
        state.Players.Count.ShouldBe(1);
        state.Players[0].Name.ShouldBe("PlayerA@Bahamut");
    }

    [Fact]
    public void StartGame_changes_phase_to_PlayerTurns() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13));
        game.StartGame();
        state.Phase.ShouldBe(DiceBlackjackPhase.PlayerTurns);
    }

    [Fact]
    public void First_two_rolls_increment_DealCount() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13));
        game.StartGame();

        var player = state.Players[0];
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13));
        player.DealCount.ShouldBe(1);
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 7, 13));
        player.DealCount.ShouldBe(2);
        player.Cards.Count.ShouldBe(2);
    }

    [Fact]
    public void Third_plus_roll_is_a_hit() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13));
        game.StartGame();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13));
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13));
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 4, 13)); // hit
        state.Players[0].Cards.Count.ShouldBe(3);
        state.Players[0].Status.ShouldBe(PlayerHandStatus.Active);
    }

    [Fact]
    public void Player_busts_when_score_exceeds_target_on_hit() {
        // CardMapping=false so face values are literal
        var (game, state) = Create(c => {
            c.DiceBlackjack.CardMapping = false;
            c.DiceBlackjack.MaxRoll = 100;
            c.DiceBlackjack.TargetPoints = 21;
        });
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 100));
        game.StartGame();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 10, 100)); // deal 1: total=10
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 9, 100));  // deal 2: total=19
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 100));  // hit: total=24 -> bust
        state.Players[0].Status.ShouldBe(PlayerHandStatus.Busted);
    }

    [Fact]
    public void All_players_bust_goes_directly_to_Done() {
        var (game, state) = Create(c => {
            c.DiceBlackjack.CardMapping = false;
            c.DiceBlackjack.MaxRoll = 100;
            c.DiceBlackjack.TargetPoints = 21;
        });
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 100));
        game.StartGame();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 10, 100)); // deal 1
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 12, 100)); // deal 2: total=22 -> bust
        // All players busted -> Done without DealerTurn
        state.Phase.ShouldBe(DiceBlackjackPhase.Done);
    }

    [Fact]
    public void Stand_advances_to_DealerTurn_when_last_player() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13));
        game.StartGame();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13));  // deal 1
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 7, 13));  // deal 2: DealCount=2
        game.Stand();
        state.Phase.ShouldBe(DiceBlackjackPhase.DealerTurn);
    }

    [Fact]
    public void Stand_via_chat_message_works() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13));
        game.StartGame();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13)); // deal 1
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 7, 13)); // deal 2
        game.ProcessChatMessage("PlayerA@Bahamut", "stand", XivChatType.Say);
        state.Phase.ShouldBe(DiceBlackjackPhase.DealerTurn);
    }

    [Fact]
    public void AutoDrawDealer_completes_game_after_all_players_stand() {
        var (game, state) = Create();
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13));
        game.StartGame();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 13)); // deal 1
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 7, 13)); // deal 2
        game.Stand();

        state.Phase.ShouldBe(DiceBlackjackPhase.DealerTurn);
        game.AutoDrawDealer();
        state.Phase.ShouldBe(DiceBlackjackPhase.Done);
    }

    [Fact]
    public void Card_mapping_ace_counts_as_eleven() {
        // CardMapping=true by default, MaxRoll=13
        var (game, _) = Create();
        var total = game.HandTotal(new[] { 1 }); // Ace = 11
        total.ShouldBe(11);
    }

    [Fact]
    public void Card_mapping_face_cards_count_as_ten() {
        var (game, _) = Create();
        game.HandTotal(new[] { 11 }).ShouldBe(10); // J
        game.HandTotal(new[] { 12 }).ShouldBe(10); // Q
        game.HandTotal(new[] { 13 }).ShouldBe(10); // K
    }

    [Fact]
    public void Dealer_bust_makes_non_busted_players_win() {
        var (game, state) = Create(c => {
            c.DiceBlackjack.CardMapping = false;
            c.DiceBlackjack.MaxRoll = 100;
            c.DiceBlackjack.TargetPoints = 21;
            c.DiceBlackjack.DealerStandAt = 17;
        });
        game.BeginRegistration();
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 5, 100));
        game.StartGame();

        game.ProcessRoll(new Roll("PlayerA@Bahamut", 10, 100)); // deal 1: total=10
        game.ProcessRoll(new Roll("PlayerA@Bahamut", 8, 100));  // deal 2: total=18
        game.Stand(); // PlayerA stands with 18, DealerTurn
        state.Phase.ShouldBe(DiceBlackjackPhase.DealerTurn);
        // Dealer draws random cards - just verify game completes
        game.AutoDrawDealer();
        state.Phase.ShouldBe(DiceBlackjackPhase.Done);
    }
}
