using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Extensions;
using GameChest.Util.ImGuiExt;

namespace GameChest;

public class FightGameSettingsWindow : Window {
    private Plugin Plugin { get; }

    private string _newPresetName = string.Empty;
    private int _selectedPresetIdx = -1;

    public FightGameSettingsWindow(Plugin plugin)
        : base("Fight Club - Settings###FightGameSettingsWindow") {
        Plugin = plugin;

        Size = ImGuiHelpers.ScaledVector2(400, 460);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
    }

    public override void Draw() {
        var cfg = Plugin.Config.FightGame;

        using (ImGuiGroupPanel.BeginGroupPanel("Stats")) {
            var w = 80f * ImGuiHelpers.GlobalScale;

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Player A");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(w);
            var hpA = cfg.PlayerAHealth;
            if (ImGui.InputInt("HP##PlayerAHealth", ref hpA, 1, 10)) {
                cfg.PlayerAHealth = Math.Clamp(hpA, 1, 9999);
                Plugin.Config.Save();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(w);
            var mpA = cfg.PlayerAMp;
            if (ImGui.InputInt("MP##PlayerAMp", ref mpA, 1, 10)) {
                cfg.PlayerAMp = Math.Clamp(mpA, 0, 9999);
                Plugin.Config.Save();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Player B");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(w);
            var hpB = cfg.PlayerBHealth;
            if (ImGui.InputInt("HP##PlayerBHealth", ref hpB, 1, 10)) {
                cfg.PlayerBHealth = Math.Clamp(hpB, 1, 9999);
                Plugin.Config.Save();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(w);
            var mpB = cfg.PlayerBMp;
            if (ImGui.InputInt("MP##PlayerBMp", ref mpB, 1, 10)) {
                cfg.PlayerBMp = Math.Clamp(mpB, 0, 9999);
                Plugin.Config.Save();
            }
        }

        ImGui.Spacing();

        using (ImGuiGroupPanel.BeginGroupPanel("Fight")) {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Max Roll");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var maxRoll = cfg.MaxRollAllowed;
            if (ImGui.InputInt("##MaxRollAllowed", ref maxRoll, 1, 5)) {
                cfg.MaxRollAllowed = Math.Clamp(maxRoll, 2, 999);
                Plugin.Config.Save();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Output Channel");
            ImGui.SameLine();
            var outChannel = cfg.OutputChannel;
            if (OutputChannelCombo.Draw("##OutputChannel", ref outChannel, 180f * ImGuiHelpers.GlobalScale)) {
                cfg.OutputChannel = outChannel;
                Plugin.Config.Save();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Join Game Phrase");
            var joinPhrase = cfg.JoinGamePhrase;
            if (ImGui.InputTextWithHint("##JoinGamePhrase", "e.g. I want to fight!", ref joinPhrase, 255, ImGuiInputTextFlags.AutoSelectAll)) {
                cfg.JoinGamePhrase = joinPhrase;
                Plugin.Config.Save();
            }
        }

        ImGui.Spacing();

        using (ImGuiGroupPanel.BeginGroupPanel("Timers")) {
            ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
            var regReminder = cfg.RegistrationReminderSeconds;
            if (ImGui.InputFloat("Registration Reminder (s)##RegReminder", ref regReminder, 1f, 10f, "%.0f")) {
                cfg.RegistrationReminderSeconds = Math.Max(0f, regReminder);
                Plugin.Config.Save();
            }

            ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
            var inactReminder = cfg.InactivityReminderSeconds;
            if (ImGui.InputFloat("Inactivity Reminder (s)##InactReminder", ref inactReminder, 1f, 10f, "%.0f")) {
                cfg.InactivityReminderSeconds = Math.Max(0f, inactReminder);
                Plugin.Config.Save();
            }

            ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
            var oot = cfg.OutOfTurnCooldownSeconds;
            if (ImGui.InputFloat("Out-of-Turn Cooldown (s)##OOTCooldown", ref oot, 0.5f, 5f, "%.1f")) {
                cfg.OutOfTurnCooldownSeconds = Math.Max(0f, oot);
                Plugin.Config.Save();
            }
        }

        ImGui.Spacing();

        using (ImGuiGroupPanel.BeginGroupPanel("Auto Mode")) {
            var automode = cfg.Automode;
            if (ImGui.Checkbox("Enabled##Automode", ref automode)) {
                cfg.Automode = automode;
                Plugin.Config.Save();
            }

            if (cfg.Automode) {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
                var delay = cfg.AutoSendDelaySeconds;
                if (ImGui.InputFloat("Delay (s)##AutoDelay", ref delay, 0.1f, 1f, "%.2f")) {
                    cfg.AutoSendDelaySeconds = Math.Max(0.1f, delay);
                    Plugin.Config.Save();
                }
            }
        }

        ImGui.Spacing();
        DrawPresets(cfg);
    }

    private void DrawPresets(Configuration.FightGameConfiguration cfg) {
        using (ImGuiGroupPanel.BeginGroupPanel("Presets")) {
            if (cfg.Presets.Count == 0) {
                ImGui.TextDisabled("(no presets saved)");
            } else {
                if (_selectedPresetIdx >= cfg.Presets.Count) _selectedPresetIdx = -1;

                var preview = _selectedPresetIdx >= 0 ? cfg.Presets[_selectedPresetIdx].Name : "(select preset)";
                ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
                if (ImGui.BeginCombo("##PresetsList", preview)) {
                    for (var i = 0; i < cfg.Presets.Count; i++) {
                        if (ImGui.Selectable(cfg.Presets[i].Name + $"##p{i}", _selectedPresetIdx == i))
                            _selectedPresetIdx = i;
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();

                using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonSuccessnNormal)
                    .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonSuccessHovered)
                    .Push(ImGuiCol.ButtonActive, Style.Components.ButtonSuccessActive)) {
                    if (ImGui.Button("Apply##ApplyPreset") && _selectedPresetIdx >= 0) {
                        cfg.ApplyPreset(cfg.Presets[_selectedPresetIdx]);
                        cfg.ActivePresetIndex = _selectedPresetIdx;
                        Plugin.Config.Save();
                    }
                }
                ImGui.SameLine();

                using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonDangerNormal)
                    .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonDangerHovered)
                    .Push(ImGuiCol.ButtonActive, Style.Components.ButtonDangerActive)) {
                    if (ImGui.Button("Delete##DeletePreset") && _selectedPresetIdx >= 0 && ImGui.GetIO().KeyCtrl) {
                        cfg.Presets.RemoveAtSafe(_selectedPresetIdx);
                        if (_selectedPresetIdx >= cfg.Presets.Count) _selectedPresetIdx = -1;
                        Plugin.Config.Save();
                    }
                }
                ImGuiUtil.ToolTip("Ctrl+Click to delete.");
            }

            ImGui.Spacing();
            ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint("##NewPresetName", "Preset name...", ref _newPresetName, 100);
            ImGui.SameLine();
            if (ImGui.Button("Save As Preset##SavePreset") && !string.IsNullOrWhiteSpace(_newPresetName)) {
                cfg.Presets.Add(cfg.SaveAsPreset(_newPresetName.Trim()));
                _selectedPresetIdx = cfg.Presets.Count - 1;
                _newPresetName = string.Empty;
                Plugin.Config.Save();
            }
        }
    }
}
