namespace GameChest;

public interface IGameState {
    bool IsActive { get; }
    void Reset();
}
