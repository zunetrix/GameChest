namespace GameChest;

public interface IGameState {
    bool IsActive { get; }
    GamePhase Phase { get; }
    void Reset();
}
