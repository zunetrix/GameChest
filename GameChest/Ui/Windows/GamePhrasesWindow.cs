using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Extensions;

namespace GameChest;

public class GamePhrasesWindow : Window {
    private Plugin Plugin { get; }

    private int _selectedGameIndex;
    private int _selectedCategoryIndex = -1;
    private int _editingPhraseIndex = -1;
    private string _newPhraseText = string.Empty;
    private float _newPhraseWeight = 1.0f;
    private string _searchPhrase = string.Empty;
    private string? _previewText;

    private readonly IGame[] _games;
    private readonly string[] _gameNames;

    private IGame SelectedGame => _games[_selectedGameIndex];
    private IReadOnlyList<PhraseCategoryMeta> Categories => SelectedGame.PhraseCategories;
    private List<PhrasePool> ConfigPhrases => SelectedGame.ConfigPhrases;
    private PhraseCollection ActiveCollection => SelectedGame.Phrases;

    public GamePhrasesWindow(Plugin plugin) : base("Game Phrases###GamePhrasesWindow") {
        Plugin = plugin;
        _games = new IGame[] {
            plugin.GameManager.FightGame,
            plugin.GameManager.PrizeRollGame,
            plugin.GameManager.DeathRollGame,
            plugin.GameManager.DeathRollTournamentGame,
            plugin.GameManager.WordGuessGame,
        };
        _gameNames = _games.Select(g => g.Name).ToArray();
        Size = ImGuiHelpers.ScaledVector2(700, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
    }

    public void OpenToGame(GameMode mode, string? categoryId = null) {
        var idx = Array.FindIndex(_games, g => g.Mode == mode);
        if (idx < 0) return;
        _selectedGameIndex = idx;
        _selectedCategoryIndex = categoryId != null
            ? Array.FindIndex(SelectedGame.PhraseCategories.ToArray(), c => c.Id == categoryId)
            : -1;
        _editingPhraseIndex = -1;
        _newPhraseText = string.Empty;
        _newPhraseWeight = 1.0f;
        _searchPhrase = string.Empty;
        _previewText = null;
        IsOpen = true;
    }

    private void SaveAndReload() {
        Plugin.Config.Save();
        SelectedGame.ReloadPhrases();
    }

    private void ResetCategoryToDefaults(PhraseCategoryMeta meta) {
        var pools = ConfigPhrases;
        pools.RemoveAll(p => p.CategoryId == meta.Id);
        pools.Add(new PhrasePool {
            CategoryId = meta.Id,
            Enabled = meta.DefaultEnabled,
            Phrases = meta.Defaults.Select(d => new WeightedPhrase { Text = d, Weight = 1.0f }).ToList(),
        });
        _editingPhraseIndex = -1;
        _newPhraseText = string.Empty;
        _newPhraseWeight = 1.0f;
        _previewText = null;
        SaveAndReload();
    }

    public override void Draw() {
        ImGui.Text("Game:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("##GameSelect", ref _selectedGameIndex, _gameNames, _gameNames.Length)) {
            _selectedCategoryIndex = -1;
            _editingPhraseIndex = -1;
            _searchPhrase = string.Empty;
            _previewText = null;
        }

        ImGui.Separator();

        var categories = Categories;
        if (categories.Count == 0) return;

        var totalWidth = ImGui.GetContentRegionAvail().X;
        var leftWidth = 180f * ImGuiHelpers.GlobalScale;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var rightWidth = totalWidth - leftWidth - spacing;

        ImGui.BeginChild("##LeftPanel", new Vector2(leftWidth, -1), false);
        if (ImGui.BeginListBox("##Categories", new Vector2(-1, -1))) {
            for (var i = 0; i < categories.Count; i++) {
                var catPool = ConfigPhrases.FirstOrDefault(p => p.CategoryId == categories[i].Id);
                var catEnabled = catPool?.Enabled ?? categories[i].DefaultEnabled;
                using var col = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), !catEnabled);
                if (ImGui.Selectable(categories[i].DisplayName + $"##c{i}", _selectedCategoryIndex == i)) {
                    _selectedCategoryIndex = i;
                    _editingPhraseIndex = -1;
                    _newPhraseText = string.Empty;
                    _newPhraseWeight = 1.0f;
                    _searchPhrase = string.Empty;
                    _previewText = null;
                }
            }
            ImGui.EndListBox();
        }
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##RightPanel", new Vector2(rightWidth, -1), false);
        if (_selectedCategoryIndex >= 0 && _selectedCategoryIndex < categories.Count)
            DrawCategoryDetail(categories[_selectedCategoryIndex]);
        else
            ImGui.TextDisabled("Select a category on the left.");
        ImGui.EndChild();
    }

    private void DrawCategoryDetail(PhraseCategoryMeta meta) {
        using (ImRaii.Group())
            DrawCategoryHeader(meta);

        // fetch after header - header may have created the pool via EnsurePool
        var pool = ConfigPhrases.FirstOrDefault(p => p.CategoryId == meta.Id);

        ImGui.BeginChild("##PhraseListScrollable", new Vector2(-1, -1), false);

        if (pool == null || pool.Phrases.Count == 0) {
            ImGui.TextDisabled("(no phrases)");
        } else {
            var useSequence = pool.UseSequence;
            var filtered = pool.Phrases
                .Select((p, i) => (p, i))
                .Where(x => string.IsNullOrWhiteSpace(_searchPhrase) ||
                            x.p.Text.Contains(_searchPhrase, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtered.Count == 0) {
                ImGui.TextDisabled("(no matches)");
            } else if (ImGui.BeginTable("##PhrasesTable", 4,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX |
                ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV)) {

                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Phrase", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("W", ImGuiTableColumnFlags.WidthFixed, 36 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 52 * ImGuiHelpers.GlobalScale);
                ImGui.TableHeadersRow();

                int? deleteIdx = null;
                int? moveFrom = null, moveTo = null;

                for (var fi = 0; fi < filtered.Count; fi++) {
                    var (phrase, origIdx) = filtered[fi];
                    var isEditing = _editingPhraseIndex == origIdx;

                    ImGui.PushID(origIdx);
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"{fi + 1:00}");

                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, Style.Colors.Green, isEditing))
                        ImGui.Selectable(phrase.Text, false, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, 0));

                    if (ImGui.BeginDragDropSource()) {
                        unsafe {
                            var idx = origIdx;
                            ImGui.SetDragDropPayload("DND_PHRASE", new ReadOnlySpan<byte>(&idx, sizeof(int)), ImGuiCond.None);
                            ImGui.Button($"({fi + 1:00}) {phrase.Text}");
                        }
                        ImGui.EndDragDropSource();
                    }

                    ImGui.PushStyleColor(ImGuiCol.DragDropTarget, Style.Components.DragDropTarget);
                    if (ImGui.BeginDragDropTarget()) {
                        var payload = ImGui.AcceptDragDropPayload("DND_PHRASE");
                        bool isDropping;
                        unsafe { isDropping = !payload.IsNull; }
                        if (isDropping && payload.IsDelivery()) {
                            unsafe {
                                moveFrom = *(int*)payload.Data;
                                moveTo = origIdx;
                            }
                        }
                        ImGui.EndDragDropTarget();
                    }
                    ImGui.PopStyleColor();

                    ImGui.TableNextColumn();
                    using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), useSequence))
                        ImGui.Text($"{phrase.Weight:0.#}");

                    ImGui.TableNextColumn();
                    if (ImGuiUtil.IconButton(FontAwesomeIcon.PencilAlt, $"##edit{origIdx}", "Edit")) {
                        _editingPhraseIndex = origIdx;
                        _newPhraseText = phrase.Text;
                        _newPhraseWeight = phrase.Weight;
                        _previewText = null;
                    }
                    ImGui.SameLine();
                    using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
                        .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
                        .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
                        if (ImGuiUtil.IconButton(FontAwesomeIcon.Trash, $"##del{origIdx}", "Remove")) {
                            if (ImGui.GetIO().KeyCtrl)
                                deleteIdx = origIdx;
                        }
                        ImGuiUtil.ToolTip("Ctrl+Click to remove.");
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();

                if (moveFrom.HasValue && moveTo.HasValue && moveFrom.Value != moveTo.Value) {
                    pool.Phrases.MoveItemToIndex(moveFrom.Value, moveTo.Value);
                    SaveAndReload();
                } else if (deleteIdx.HasValue) {
                    pool.Phrases.RemoveAtSafe(deleteIdx.Value);
                    if (_editingPhraseIndex == deleteIdx.Value) {
                        _editingPhraseIndex = -1;
                        _newPhraseText = string.Empty;
                        _newPhraseWeight = 1.0f;
                    } else if (_editingPhraseIndex > deleteIdx.Value) {
                        _editingPhraseIndex--;
                    }
                    _previewText = null;
                    SaveAndReload();
                }
            }
        }

        ImGui.EndChild();
    }

    private void DrawCategoryHeader(PhraseCategoryMeta meta) {
        var pool = ConfigPhrases.FirstOrDefault(p => p.CategoryId == meta.Id);

        ImGui.Text(meta.DisplayName);
        ImGui.SameLine();
        var enabled = pool?.Enabled ?? meta.DefaultEnabled;
        if (ImGui.Checkbox("Enabled##CategoryEnabled", ref enabled)) {
            (pool ?? EnsurePool(meta.Id)).Enabled = enabled;
            SaveAndReload();
        }
        ImGuiUtil.ToolTip("Auto send phrases in each game stage");

        ImGui.SameLine();
        var useSeq = pool?.UseSequence ?? false;
        if (ImGui.Checkbox("Sequence##CategorySequence", ref useSeq)) {
            (pool ?? EnsurePool(meta.Id)).UseSequence = useSeq;
            SaveAndReload();
        }
        ImGuiUtil.ToolTip("Play phrases in fixed order instead of randomly.\nDrag rows to reorder.");
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
            if (ImGuiUtil.IconButton(FontAwesomeIcon.Undo, "##ResetDefaults", "Reset to defaults")) {
                if (ImGui.GetIO().KeyCtrl)
                    ResetCategoryToDefaults(meta);
            }
            ImGuiUtil.ToolTip("Ctrl+Click to reset this category to defaults.");
        }
        if (meta.Variables.Length > 0) {
            ImGui.TextDisabled($"Variables:  {string.Join("  ", meta.Variables)}");
        }

        ImGui.Separator();

        // Add / Edit area
        var isUpdateMode = _editingPhraseIndex >= 0;
        var isUpdateLabel = isUpdateMode ? "Update" : "Add";

        var multilineH = ImGui.GetTextLineHeight() * 3 + ImGui.GetStyle().FramePadding.Y * 4;
        ImGui.InputTextMultiline("##PhraseText", ref _newPhraseText, 2000, new Vector2(-1, multilineH));

        using (ImRaii.Disabled(pool?.UseSequence ?? false)) {
            ImGui.Text("Phrase Weight");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
            ImGui.InputFloat("##PhraseWeight", ref _newPhraseWeight, 0.1f, 1f, "%.1f");
        }

        ImGui.SameLine();
        if (ImGui.Button($"{isUpdateLabel}##UpsertPhrase") && !string.IsNullOrWhiteSpace(_newPhraseText)) {
            pool ??= EnsurePool(meta.Id);
            if (isUpdateMode) {
                pool.Phrases[_editingPhraseIndex] = new WeightedPhrase {
                    Text = _newPhraseText.Trim(), Weight = MathF.Max(0.1f, _newPhraseWeight),
                };
                _editingPhraseIndex = -1;
            } else {
                pool.Phrases.Add(new WeightedPhrase {
                    Text = _newPhraseText.Trim(), Weight = MathF.Max(0.1f, _newPhraseWeight),
                });
            }
            _newPhraseText = string.Empty;
            _newPhraseWeight = 1.0f;
            SaveAndReload();
        }

        if (isUpdateMode) {
            ImGui.SameLine();
            if (ImGui.Button("Cancel##CancelEdit")) {
                _editingPhraseIndex = -1;
                _newPhraseText = string.Empty;
                _newPhraseWeight = 1.0f;
            }
        }

        ImGui.Spacing();
        if (ImGui.Button("Test random phrase")) {
            var vars = meta.Variables.ToDictionary(
                v => v.Trim('{', '}'),
                v => $"<{v.Trim('{', '}')}>");
            _previewText = ActiveCollection.GetRandomPhrase(meta.Id, vars) ?? "(no phrase configured)";
        }
        if (_previewText != null) {
            ImGui.TextWrapped(_previewText);
        }

        ImGui.Spacing();
        ImGui.Separator();

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##PhraseSearch", "Search phrases...", ref _searchPhrase, 200);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    private PhrasePool EnsurePool(string categoryId) {
        var pool = ConfigPhrases.FirstOrDefault(p => p.CategoryId == categoryId);
        if (pool != null) return pool;
        pool = new PhrasePool { CategoryId = categoryId };
        ConfigPhrases.Add(pool);
        return pool;
    }
}
