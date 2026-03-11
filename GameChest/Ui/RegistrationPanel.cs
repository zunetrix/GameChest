using System;
using System.Collections.Generic;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using GameChest.Extensions;
using GameChest.Resources;

namespace GameChest;

/// <summary>Shared registration panel: count header, add/target input, and player table with remove/block buttons.</summary>
public static class RegistrationPanel {
    private static bool _showFullName = false;

    public static void Draw(
        string id,
        IList<string> players,
        ref string inputBuffer,
        int minPlayers,
        Action<string> onAdd,
        Plugin plugin) {

        var scale = ImGuiHelpers.GlobalScale;

        // Count header
        using (ImRaii.PushColor(ImGuiCol.Text,
            players.Count >= minPlayers ? Style.Colors.Green : Style.Colors.Gray))
            ImGui.Text($"Players: {players.Count}");
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
            ImGui.Text($"(min {minPlayers})");
        ImGui.Spacing();

        // Input + Add + Target
        ImGui.SetNextItemWidth(250f * scale);
        ImGui.InputTextWithHint($"##{id}Input", "Firstname Lastname[@World]", ref inputBuffer, 64);

        ImGui.SameLine();
        using (ImRaii.Disabled(string.IsNullOrWhiteSpace(inputBuffer)))
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (ImGui.Button($"{Language.Add}##{id}Add")) {
                onAdd(inputBuffer.Trim());
                inputBuffer = string.Empty;
            }
        }
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Crosshairs, $"##{id}Target", "Add targeted player")) {
            var name = GameTargetManager.GetTargetPlayerFullName();
            if (name != null) onAdd(name);
        }
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
        ImGui.Checkbox($"@World##{id}FullName", ref _showFullName);
        ImGuiUtil.ToolTip("Show full name");

        ImGui.Spacing();

        if (players.Count == 0) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text("(no players registered)");
            return;
        }

        // Player table
        var btnW = ImGui.GetFrameHeight();
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        using var table = ImRaii.Table($"##{id}RegTable", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("##Num", ImGuiTableColumnFlags.WidthFixed, 24f * scale);
        ImGui.TableSetupColumn("##Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##Btns", ImGuiTableColumnFlags.WidthFixed, btnW * 2 + spacing);

        string? toRemove = null;
        string? toBlock = null;

        for (var i = 0; i < players.Count; i++) {
            var p = players[i];
            ImGui.PushID(i);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text($"{i + 1}");

            ImGui.TableNextColumn();
            ImGui.Text(_showFullName ? p : ShortName(p));

            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Times, "##Rem", "Remove"))
                    toRemove = p;

                ImGui.SameLine();
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Ban, "##Blk", $"{Language.Block} (Ctrl+Click)") && ImGui.GetIO().KeyCtrl)
                    toBlock = p;
            }

            ImGui.PopID();
        }

        if (toRemove != null)
            players.Remove(toRemove);

        if (toBlock != null) {
            players.Remove(toBlock);
            if (!plugin.Config.Blocklist.ContainsPlayer(toBlock)) {
                plugin.Config.Blocklist.Add(toBlock);
                plugin.Config.Save();
            }
        }
    }

    private static string ShortName(string s) {
        var i = s.IndexOf('@');
        return i >= 0 ? s[..i] : s;
    }
}
