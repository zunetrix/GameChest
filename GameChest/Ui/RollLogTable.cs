using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace GameChest;

public static class RollLogTable {
    public static void Draw(string id, IReadOnlyList<Roll> log, Vector4 rollColor) {
        using var table = ImRaii.Table(id, 4,
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 65f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthFixed, 140f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        for (var i = log.Count - 1; i >= 0; i--) {
            var e = log[i];
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text($"{i + 1}");

            ImGui.TableNextColumn();
            ImGui.Text(e.At.ToString("HH:mm:ss"));

            ImGui.TableNextColumn();
            ImGui.Text(ShortName(e.PlayerName));

            ImGui.TableNextColumn();
            var rollText = e.OutOf > 0 ? $"{e.Result}/{e.OutOf}" : $"{e.Result}";
            // using (ImRaii.PushColor(ImGuiCol.Text, rollColor))
            ImGui.Text(rollText);
        }
    }

    private static string ShortName(string fullName) {
        var at = fullName.IndexOf('@');
        return at >= 0 ? fullName[..at] : fullName;
    }
}
