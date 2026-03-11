using System.Collections.Generic;
using System.Linq;

namespace GameChest;

public enum DiceBlackjackPhase { Idle, Registration, PlayerTurns, DealerTurn, Done }
public enum PlayerHandStatus { Active, Standing, Busted }

public class DiceBlackjackPlayerHand {
    public string Name { get; }
    public List<int> Cards { get; } = new();
    public PlayerHandStatus Status { get; set; } = PlayerHandStatus.Active;
    public int DealCount { get; set; } = 0;

    public DiceBlackjackPlayerHand(string name) { Name = name; }
}

public class DiceBlackjackState : IGameState {
    public DiceBlackjackPhase Phase { get; set; } = DiceBlackjackPhase.Idle;
    public bool IsActive => Phase is not (DiceBlackjackPhase.Idle or DiceBlackjackPhase.Done);
    public List<DiceBlackjackPlayerHand> Players { get; } = new();
    public int CurrentPlayerIndex { get; set; } = 0;
    public DiceBlackjackPlayerHand? CurrentPlayer =>
        Phase == DiceBlackjackPhase.PlayerTurns && CurrentPlayerIndex < Players.Count
            ? Players[CurrentPlayerIndex]
            : null;
    public List<int> DealerCards { get; } = new();
    public PlayerHandStatus DealerStatus { get; set; } = PlayerHandStatus.Active;
    public string? Winner { get; set; }

    public void Reset() {
        Phase = DiceBlackjackPhase.Idle;
        Players.Clear();
        CurrentPlayerIndex = 0;
        DealerCards.Clear();
        DealerStatus = PlayerHandStatus.Active;
        Winner = null;
    }
}
