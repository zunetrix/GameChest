// Replaces DalamudApi/DalamudApi.cs in the GameChest2 (test) compilation.
// All properties return no-op stubs so game logic can run without a running FFXIV client.
using Dalamud.Plugin.Services;
using Moq;

namespace GameChest;

public class DalamudApi {
    private static readonly IPluginLog _log;
    private static readonly IPlayerState _playerState;

    static DalamudApi() {
        _log = new Mock<IPluginLog>().Object;

        var ps = new Mock<IPlayerState>();
        ps.Setup(p => p.CharacterName).Returns("TestGM");
        _playerState = ps.Object;
    }

    public static IPluginLog PluginLog => _log;
    public static IPlayerState PlayerState => _playerState;
}
