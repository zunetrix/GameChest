using Dalamud.Game.ClientState.Objects.SubKinds;

using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace GameChest;

public static class GameTargetManager {

    public static string? GetTargetPlayerFullName() {
        var target = DalamudApi.TargetManager.Target;
        if (target == null || target.ObjectKind != ObjectKind.Pc)
            return null;

        var player = target as IPlayerCharacter;
        if (player == null) return null;

        var world = player.HomeWorld.ValueNullable?.Name;
        return world != null
            ? $"{player.Name.TextValue}@{world}"
            : player.Name.TextValue;
    }
}
