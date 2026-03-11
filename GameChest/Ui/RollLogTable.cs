using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace GameChest;

public static class RollLogTable {
    private enum Sort { Chronological, Highest, Lowest }
    private static Sort _sort = Sort.Chronological;

    public static void Draw(string id, IReadOnlyList<Roll> log, Vector4 rollColor) {
        // Sort toggle buttons
        DrawSortButtons();
        ImGui.Spacing();

        using var table = ImRaii.Table(id, 4,
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 65f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthFixed, 140f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        var entries = _sort switch {
            Sort.Highest => log.OrderByDescending(e => e.Result).ToList(),
            Sort.Lowest => log.OrderBy(e => e.Result).ToList(),
            _ => Enumerable.Range(0, log.Count).Reverse().Select(i => log[i]).ToList(),
        };

        for (var i = 0; i < entries.Count; i++) {
            var e = entries[i];
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text($"{i + 1}");

            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Components.TextDisabled))
                ImGui.Text(e.At.ToString("HH:mm:ss"));

            ImGui.TableNextColumn();
            ImGui.Text(ShortName(e.PlayerName));

            ImGui.TableNextColumn();
            var rollText = e.OutOf > 0 ? $"{e.Result}/{e.OutOf}" : $"{e.Result}";
            ImGui.Text(rollText);
        }
    }

    private static void DrawSortButtons() {
        DrawToggle("Chronological", Sort.Chronological);
        ImGui.SameLine();
        DrawToggle("Highest", Sort.Highest);
        ImGui.SameLine();
        DrawToggle("Lowest", Sort.Lowest);
    }

    private static void DrawToggle(string label, Sort value) {
        var active = _sort == value;
        using (active
            ? ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonBlueNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonBlueHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonBlueActive)
            : ImRaii.PushColor(ImGuiCol.Button, Style.Components.Button)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonActive)) {
            if (ImGui.SmallButton($"{label}##RltSort{value}"))
                _sort = value;
        }
    }

    private static string ShortName(string fullName) {
        var at = fullName.IndexOf('@');
        return at >= 0 ? fullName[..at] : fullName;
    }
}
