namespace GameChest;

/// <summary>
/// Minimal interface for the plugin services that game logic needs.
/// Allows games to be instantiated and tested without the full Dalamud plugin stack.
/// </summary>
internal interface IPluginContext {
    Configuration Config { get; }
    RollManager? RollManager { get; }
}
