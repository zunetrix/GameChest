using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GameChest;

public class WordGuessQuestionListWindow : Window {
    private Plugin Plugin { get; }

    private int _selectedIndex = -1;
    private string _editQuestion = string.Empty;
    private string _editAnswer = string.Empty;
    private string _editHint = string.Empty;
    private bool _editHasHint = false;
    private bool _editHasTimer = false;
    private int _editTimerSecs = 60;
    private bool _isNewItem = false;

    public WordGuessQuestionListWindow(Plugin plugin)
        : base("Word Guess - Question List###WordGuessQuestionListWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(640, 440);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    private List<Configuration.WordGuessQuestion> Questions => Plugin.Config.WordGuess.Questions;

    public override void Draw() {
        var totalWidth = ImGui.GetContentRegionAvail().X;
        var leftWidth = 200f * ImGuiHelpers.GlobalScale;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var rightWidth = totalWidth - leftWidth - spacing;

        // Left: list
        ImGui.BeginChild("##WqLeft", new Vector2(leftWidth, -1), false);
        DrawQuestionList();
        ImGui.EndChild();

        ImGui.SameLine();

        // Right: edit form
        ImGui.BeginChild("##WqRight", new Vector2(rightWidth, -1), false);
        if (_selectedIndex >= 0 || _isNewItem)
            DrawEditForm();
        else
            ImGui.TextDisabled("Select a question or click Add New.");
        ImGui.EndChild();
    }

    private void DrawQuestionList() {
        // Header: count + Add New button
        ImGui.Text($"Questions ({Questions.Count})");
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.Plus, "##WqAddNew", "Add New Question")) {
                _isNewItem = true;
                _selectedIndex = -1;
                _editQuestion = string.Empty;
                _editAnswer = string.Empty;
                _editHint = string.Empty;
                _editHasHint = false;
                _editHasTimer = false;
                _editTimerSecs = 60;
            }
        }

        if (Questions.Count == 0) {
            ImGui.Spacing();
            ImGui.TextDisabled("No questions yet.");
            return;
        }

        ImGui.Separator();

        if (ImGui.BeginListBox("##WqList", new Vector2(-1, -1))) {
            for (var i = 0; i < Questions.Count; i++) {
                var q = Questions[i];
                var label = $"{i + 1:00}. {Truncate(q.Question, 24)}##wqitem{i}";
                var isSelected = _selectedIndex == i && !_isNewItem;
                if (ImGui.Selectable(label, isSelected)) {
                    _selectedIndex = i;
                    _isNewItem = false;
                    LoadForEdit(q);
                }
            }
            ImGui.EndListBox();
        }
    }

    private void DrawEditForm() {
        var isNew = _isNewItem;
        ImGui.Text(isNew ? "New Question" : $"Question {_selectedIndex + 1}");
        ImGui.Separator();
        ImGui.Spacing();

        // Question text
        ImGui.Text("Question *");
        ImGuiUtil.ToolTip("What is shown/announced to the players.");
        ImGui.SetNextItemWidth(-1);
        var multiH = ImGui.GetTextLineHeight() * 3 + ImGui.GetStyle().FramePadding.Y * 4;
        ImGui.InputTextMultiline("##WqQuestion", ref _editQuestion, 500, new Vector2(-1, multiH));

        ImGui.Spacing();

        // Answer
        ImGui.Text("Answer *");
        ImGuiUtil.ToolTip("Exact text players must type to win.");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##WqAnswer", ref _editAnswer, 200);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Hint
        ImGui.Checkbox("Has Hint##WqHasHint", ref _editHasHint);
        if (_editHasHint) {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##WqHint", ref _editHint, 300);
        }

        ImGui.Spacing();

        // Per-question timer override
        ImGui.Checkbox("Override Timer##WqHasTimer", ref _editHasTimer);
        ImGuiUtil.ToolTip("Override the global timer for this specific question.");
        if (_editHasTimer) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(55f * ImGuiHelpers.GlobalScale);
            var mins = _editTimerSecs / 60;
            if (ImGui.InputInt("min##WqTimerMin", ref mins, 0))
                _editTimerSecs = System.Math.Clamp(mins, 0, 99) * 60 + _editTimerSecs % 60;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(55f * ImGuiHelpers.GlobalScale);
            var secs = _editTimerSecs % 60;
            if (ImGui.InputInt("sec##WqTimerSec", ref secs, 0))
                _editTimerSecs = _editTimerSecs / 60 * 60 + System.Math.Clamp(secs, 0, 59);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Action buttons
        var canSave = !string.IsNullOrWhiteSpace(_editQuestion) && !string.IsNullOrWhiteSpace(_editAnswer);

        using (ImRaii.Disabled(!canSave)) {
            if (ImGui.Button(isNew ? "Add##WqSave" : "Update##WqSave")) {
                var q = new Configuration.WordGuessQuestion {
                    Question = _editQuestion.Trim(),
                    Answer = _editAnswer.Trim(),
                    Hint = _editHasHint && !string.IsNullOrWhiteSpace(_editHint) ? _editHint.Trim() : null,
                    TimerSecs = _editHasTimer && _editTimerSecs > 0 ? _editTimerSecs : (int?)null,
                };
                if (isNew) {
                    Questions.Add(q);
                    _selectedIndex = Questions.Count - 1;
                    _isNewItem = false;
                } else {
                    Questions[_selectedIndex] = q;
                }
                Plugin.Config.Save();
            }
        }

        if (!canSave)
            ImGuiUtil.ToolTip("Question and Answer fields are required.");

        if (!isNew) {
            ImGui.SameLine();

            // Move Up
            using (ImRaii.Disabled(_selectedIndex <= 0)) {
                if (ImGuiUtil.IconButton(FontAwesomeIcon.ArrowUp, "##WqMoveUp", "Move Up")) {
                    (Questions[_selectedIndex - 1], Questions[_selectedIndex]) =
                        (Questions[_selectedIndex], Questions[_selectedIndex - 1]);
                    _selectedIndex--;
                    Plugin.Config.Save();
                }
            }
            ImGui.SameLine();

            // Move Down
            using (ImRaii.Disabled(_selectedIndex >= Questions.Count - 1)) {
                if (ImGuiUtil.IconButton(FontAwesomeIcon.ArrowDown, "##WqMoveDown", "Move Down")) {
                    (Questions[_selectedIndex + 1], Questions[_selectedIndex]) =
                        (Questions[_selectedIndex], Questions[_selectedIndex + 1]);
                    _selectedIndex++;
                    Plugin.Config.Save();
                }
            }
            ImGui.SameLine();

            // Delete
            using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
                .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
                .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Trash, "##WqDelete", "Ctrl+Click to delete")
                    && ImGui.GetIO().KeyCtrl) {
                    Questions.RemoveAt(_selectedIndex);
                    Plugin.Config.Save();
                    _selectedIndex = System.Math.Min(_selectedIndex, Questions.Count - 1);
                    if (_selectedIndex >= 0) LoadForEdit(Questions[_selectedIndex]);
                    else { _selectedIndex = -1; }
                }
                ImGuiUtil.ToolTip("Ctrl+Click to delete.");
            }
        }
    }

    private void LoadForEdit(Configuration.WordGuessQuestion q) {
        _editQuestion = q.Question;
        _editAnswer = q.Answer;
        _editHasHint = q.Hint != null;
        _editHint = q.Hint ?? string.Empty;
        _editHasTimer = q.TimerSecs.HasValue;
        _editTimerSecs = q.TimerSecs ?? 60;
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "…";
}
