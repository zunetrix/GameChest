using Dalamud.Bindings.ImGui;

namespace GameChest.Debug;

public sealed class GeneralDebugWidget : Widget {
    public override string Title => "General";

    public GeneralDebugWidget(WidgetContext ctx) : base(ctx) {
    }

    public override void Draw() {
        ImGui.Text("Commands");
        foreach (var command in DalamudApi.CommandManager.Commands) {
            ImGui.Text($"{command.Key}");
        }
    }
}

