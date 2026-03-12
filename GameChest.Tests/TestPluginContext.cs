using Dalamud.Plugin;
using Moq;

namespace GameChest.Tests;

/// <summary>
/// Minimal IPluginContext implementation for tests.
/// Plugin.Config.Save() is a no-op (mocked IDalamudPluginInterface).
/// </summary>
internal class TestPluginContext : IPluginContext {
    public Configuration Config { get; }
    public RollManager? RollManager => null;

    public TestPluginContext(Action<Configuration>? configure = null) {
        var mockInterface = new Mock<IDalamudPluginInterface>();
        Config = new Configuration();
        Config.Initialize(mockInterface.Object);
        configure?.Invoke(Config);
    }
}
