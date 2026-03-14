using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Extensions;
using GameChest.Resources;

namespace GameChest;

public class BlocklistWindow : Window {
    private Plugin Plugin { get; }

    private string _inputName = string.Empty;
    private string _searchString = string.Empty;
    private string? _duplicateWarning;
    private List<string> _filtered = new();

    public BlocklistWindow(Plugin plugin) : base($"{Plugin.Name} Blocklist###BlocklistWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(350, 350);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
    }

    public override void OnOpen() => Search();

    private void Search() {
        _filtered = string.IsNullOrWhiteSpace(_searchString)
            ? Plugin.Config.Blocklist.ToList()
            : Plugin.Config.Blocklist
                .Where(e => e.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    public override void Draw() {
        ImGui.BeginGroup();
        DrawHeader();
        ImGui.EndGroup();

        ImGui.BeginChild("##BlocklistList", new Vector2(-1, -1), false);

        if (_filtered.Count == 0) {
            ImGui.TextDisabled("(empty)");
        } else if (ImGui.BeginTable("##BlocklistTable", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX |
            ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV)) {

            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
            ImGui.TableHeadersRow();

            string? deleteEntry = null;
            for (var i = 0; i < _filtered.Count; i++) {
                var entry = _filtered[i];
                ImGui.PushID(i);
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"{i + 1:00}");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(entry);

                ImGui.TableNextColumn();
                if (ImGuiUtil.DangerIconButton(FontAwesomeIcon.Trash, "##del", "Remove")) {
                    if (ImGui.GetIO().KeyCtrl)
                        deleteEntry = entry;
                }
                ImGuiUtil.ToolTip("Ctrl+Click to remove.");

                ImGui.PopID();
            }

            ImGui.EndTable();

            if (deleteEntry != null) {
                Plugin.Config.Blocklist.Remove(deleteEntry);
                Plugin.Config.Save();
                Search();
            }
        }

        ImGui.EndChild();
    }

    private void DrawHeader() {
        if (_duplicateWarning != null)
            ImGuiUtil.DrawColoredBanner(_duplicateWarning, Style.Colors.Red);

        if (ImGui.InputTextWithHint("##BlocklistSearch", Language.SearchInputLabel, ref _searchString, 255, ImGuiInputTextFlags.AutoSelectAll))
            Search();

        ImGui.Spacing();

        var isActive = Plugin.Config.IsBlockListActive;
        if (ImGui.Checkbox("Enable blocklist##EnableBlocklist", ref isActive)) {
            Plugin.Config.IsBlockListActive = isActive;
            Plugin.Config.Save();
        }
        ImGuiUtil.HelpMarker("When enabled, players on this list will have all their roll and chat interactions ignored. They cannot register for any game.");

        ImGui.Spacing();

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var framePad = ImGui.GetStyle().FramePadding.X;
        var addBtnW = ImGui.CalcTextSize("Add").X + framePad * 2;
        var iconBtnW = ImGui.GetFrameHeight();
        ImGui.SetNextItemWidth(MathF.Max(ImGui.GetContentRegionAvail().X - addBtnW - iconBtnW * 2 - spacing * 3, 50));

        if (ImGui.InputTextWithHint("##BlocklistInput", "Firstname Lastname@World", ref _inputName, 100, ImGuiInputTextFlags.AutoSelectAll))
            _duplicateWarning = null;

        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Crosshairs, "##AddFromTargetBtn", "Add from target")) {
            var fullName = GameTargetManager.GetTargetPlayerFullName();
            if (fullName != null)
                TryAdd(fullName);
            else
                _duplicateWarning = "No player target selected.";
        }

        ImGui.SameLine();
        if (ImGui.Button("Add##AddBlocklist")) {
            if (TryAdd(_inputName))
                _inputName = string.Empty;
        }

        ImGui.SameLine();
        if (ImGuiUtil.DangerIconButton(FontAwesomeIcon.TrashAlt, "##ClearBlocklistBtn", Language.ConfirmationInstructionTooltip)) {
            if (ImGui.GetIO().KeyCtrl) {
                Plugin.Config.Blocklist.Clear();
                Plugin.Config.Save();
                Search();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    private bool TryAdd(string input) {
        var name = Normalize(input);
        if (string.IsNullOrWhiteSpace(name)) return false;

        if (Plugin.Config.Blocklist.ContainsPlayer(name)) {
            _duplicateWarning = $"{name} already in blocklist.";
            return false;
        }

        Plugin.Config.Blocklist.Add(name);
        Plugin.Config.Save();
        _duplicateWarning = null;
        Search();
        return true;
    }

    private static string Normalize(string input) =>
        Regex.Replace(input.Trim(), @"\s+", " ");
}
