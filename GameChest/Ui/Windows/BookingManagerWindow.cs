using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class BookingManagerWindow : Window {
    private Plugin Plugin { get; }
    private string _inputBuffer = string.Empty;
    private bool _showFullName = true;
    private string _filterName = string.Empty;
    private string _filterNotes = string.Empty;

    public BookingManagerWindow(Plugin plugin) : base("Booking Manager###BookingManagerWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(480, 420);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(300, 150),
        };
    }

    public override void Draw() {
        var booking = Plugin.Config.PlayerBookingList;
        var scale = ImGuiHelpers.GlobalScale;
        var selected = booking.Count(p => p.Selected);

        // Header
        using (ImRaii.PushColor(ImGuiCol.Text, booking.Count > 0 ? Style.Colors.Green : Style.Colors.Gray))
            ImGui.Text($"Booked Players: {booking.Count}");
        if (booking.Count > 0) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text($"({selected} selected)");
        }

        ImGui.Spacing();

        // Input row
        ImGui.SetNextItemWidth(200f * scale);
        var enter = ImGui.InputTextWithHint("##BmInput", "Firstname Lastname[@World]", ref _inputBuffer, 64,
            ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        using (ImRaii.Disabled(string.IsNullOrWhiteSpace(_inputBuffer)))
            if ((ImGuiUtil.SuccessButton("Add##BmAdd") || enter) && !string.IsNullOrWhiteSpace(_inputBuffer)) {
                TryAdd(_inputBuffer.Trim(), booking);
                _inputBuffer = string.Empty;
            }
        ImGui.SameLine();
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Crosshairs, "##BmTarget", "Add targeted player")) {
            var name = GameTargetManager.GetTargetPlayerFullName();
            if (name != null) TryAdd(name, booking);
        }
        ImGui.SameLine();
        ImGui.Spacing(); ImGui.SameLine();
        ImGui.Checkbox("@World##BmWorld", ref _showFullName);
        ImGuiUtil.ToolTip("Show full name");
        ImGui.Spacing();

        if (booking.Count == 0) {
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text("(no players in booking list)");
            return;
        }

        var btnW = ImGui.GetFrameHeight();
        var spcX = ImGui.GetStyle().ItemSpacing.X;

        using var table = ImRaii.Table("##BmTable", 5,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings |
            ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn("##BmSel", ImGuiTableColumnFlags.WidthFixed, btnW);
        ImGui.TableSetupColumn("###BmNum", ImGuiTableColumnFlags.WidthFixed, 22f * scale);
        ImGui.TableSetupColumn("Name##BmName", ImGuiTableColumnFlags.WidthStretch, 2f);
        ImGui.TableSetupColumn("Notes##BmNote", ImGuiTableColumnFlags.WidthStretch, 1f);
        ImGui.TableSetupColumn("##BmBtns", ImGuiTableColumnFlags.WidthFixed, btnW * 3 + spcX * 2);

        // Header row: select-all checkbox + filter inputs for name and notes
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        ImGui.TableNextColumn();
        var allSel = booking.All(p => p.Selected);
        var checkHdr = allSel;
        if (ImGui.Checkbox("##BmSelAll", ref checkHdr)) {
            foreach (var p in booking) p.Selected = checkHdr;
            Plugin.Config.Save();
        }
        ImGuiUtil.ToolTip(allSel ? "Deselect all" : "Select all");

        ImGui.TableNextColumn();
        ImGui.TableHeader("#");

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##BmFilterName", "Seach...", ref _filterName, 64);

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##BmFilterNotes", "Seach...", ref _filterNotes, 64);

        ImGui.TableNextColumn();
        if (booking.Count > 0) {
            if (ImGuiUtil.DangerIconButton(FontAwesomeIcon.Trash, "##BmClear") && ImGui.GetIO().KeyCtrl) {
                booking.Clear();
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("Ctrl+Click to clear all");
        }

        int? moveUp = null, moveDown = null;
        int? toRemove = null;
        bool dirty = false;

        for (var i = 0; i < booking.Count; i++) {
            var p = booking[i];

            var nameOk = string.IsNullOrEmpty(_filterName) || p.FullName.Contains(_filterName, StringComparison.OrdinalIgnoreCase);
            var notesOk = string.IsNullOrEmpty(_filterNotes) || p.Notes.Contains(_filterNotes, StringComparison.OrdinalIgnoreCase);
            if (!nameOk || !notesOk) continue;

            ImGui.PushID(i);
            ImGui.TableNextRow();

            // Checkbox
            ImGui.TableNextColumn();
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, 0);
            var sel = p.Selected;
            if (ImGui.Checkbox("##BmCheck", ref sel)) { p.Selected = sel; dirty = true; }

            // Index
            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Gray))
                ImGui.Text($"{i + 1}");

            // Name
            ImGui.TableNextColumn();
            if (!p.Selected) ImGui.PushStyleColor(ImGuiCol.Text, Style.Colors.Gray);
            ImGui.Text(_showFullName ? p.FullName : PlayerName.Short(p.FullName));
            if (!p.Selected) ImGui.PopStyleColor();

            // Notes
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var notes = p.Notes;
            if (ImGui.InputTextWithHint("##BmNotes", "notes...", ref notes, 48)) { p.Notes = notes; dirty = true; }

            // Buttons
            ImGui.TableNextColumn();
            using (ImRaii.Disabled(i == 0))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.ArrowUp, "##Up", "Move up"))
                    moveUp = i;
            ImGui.SameLine();
            using (ImRaii.Disabled(i == booking.Count - 1))
                if (ImGuiUtil.IconButton(FontAwesomeIcon.ArrowDown, "##Down", "Move down"))
                    moveDown = i;
            ImGui.SameLine();
            if (ImGuiUtil.DangerIconButton(FontAwesomeIcon.Times, "##Rem", "Remove"))
                toRemove = i;

            ImGui.PopID();
        }

        if (moveUp is { } up) { (booking[up], booking[up - 1]) = (booking[up - 1], booking[up]); dirty = true; }
        if (moveDown is { } down) { (booking[down], booking[down + 1]) = (booking[down + 1], booking[down]); dirty = true; }
        if (toRemove is { } rem) { booking.RemoveAt(rem); dirty = true; }
        if (dirty) Plugin.Config.Save();
    }

    private void TryAdd(string name, List<Configuration.BookedPlayer> booking) {
        if (booking.Any(p => p.FullName == name)) return;
        booking.Add(new Configuration.BookedPlayer { FullName = name });
        Plugin.Config.Save();
    }
}
