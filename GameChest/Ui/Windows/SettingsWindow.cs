using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Resources;
using GameChest.Util;
using GameChest.Util.ImGuiExt;

namespace GameChest;

public class SettingsWindow : Window {
    private Plugin Plugin { get; }
    private string _customCommandInput = string.Empty;

    public SettingsWindow(Plugin plugin) : base($"{Plugin.Name} {Language.SettingsTitle}###SettingsWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(400, 390);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = ImGuiHelpers.ScaledVector2(100, 100),
        };
    }

    public override void OnOpen() {
        _customCommandInput = Plugin.Config.CustomCommand;
    }

    public override void Draw() {
        DrawGeneralSection();
        ImGui.Spacing();
        ImGui.Spacing();
        DrawWindowSection();
        ImGui.Spacing();
        ImGui.Spacing();
        DrawDeveloperSection();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawActionButtons();
    }

    private void DrawGeneralSection() {
        using (ImGuiGroupPanel.BeginGroupPanel(Language.SettingsGeneralTab)) {
            var listenToRollMessages = Plugin.Config.ListenToRollMessages;
            if (ImGui.Checkbox("Listen to roll messages##ListenToRollMessages", ref listenToRollMessages)) {
                Plugin.Config.ListenToRollMessages = listenToRollMessages;
                Plugin.Config.Save();
            }

            ImGui.Spacing();

            var listenToChatMessages = Plugin.Config.ListenToChatMessages;
            if (ImGui.Checkbox("Listen to chat messages##ListenToChatMessages", ref listenToChatMessages)) {
                Plugin.Config.ListenToChatMessages = listenToChatMessages;
                Plugin.Config.Save();
            }

            if (ImGui.CollapsingHeader("Allowed Chats")) {
                ImGui.Indent();
                if (ImGui.BeginCombo("##ListenedChatTypesSelectList", "Select Chat to Listen")) {
                    foreach (var chatType in Plugin.ChatWatcher.AllowedChatTypes.Except(Plugin.Config.ListenedChatTypes)) {
                        if (ImGui.Selectable($"{chatType}", false)) {
                            Plugin.Config.ListenedChatTypes.Add(chatType);
                            Plugin.Config.Save();
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.Spacing();
                ImGui.Text("Listened Chats");
                if (ImGui.BeginListBox("##ListenedChatTypes", new Vector2(-1, 100))) {
                    foreach (var chatType in Plugin.Config.ListenedChatTypes.ToList()) {
                        if (ImGui.Selectable($"{chatType}", false) && ImGui.GetIO().KeyCtrl) {
                            Plugin.Config.ListenedChatTypes.Remove(chatType);
                            Plugin.Config.Save();
                        }
                        ImGuiUtil.ToolTip(Language.ConfirmationInstructionTooltip);
                    }
                    ImGui.EndListBox();
                }
                ImGui.Unindent();
            }

            ImGui.Spacing();

            ImGui.Text("Highlight Color");
            ImGui.SetNextItemWidth(250);
            Plugin.Config.HighlightColor = ImGuiComponents.ColorPickerWithPalette(1, "##MacroColorInput", Plugin.Config.HighlightColor);
            ImGui.SameLine();
            if (ImGuiUtil.IconButton(FontAwesomeIcon.Undo, "##ResetHighlightColorBtn", "Reset")) {
                Plugin.Config.HighlightColor = Style.Colors.Yellow;
            }

            ImGui.Spacing();

            ImGui.Text("Custom Command");
            ImGuiUtil.ToolTip("Optional alias command (e.g. /fclub).");
            ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint("##CustomCommand", "/fclub", ref _customCommandInput, 32);
            ImGui.SameLine();
            if (ImGui.Button("Apply##ApplyCustomCommand")) {
                if (string.IsNullOrWhiteSpace(_customCommandInput))
                    Plugin.PluginCommandManager.ClearCustomCommand();
                else
                    Plugin.PluginCommandManager.SetCustomCommand(_customCommandInput);
                _customCommandInput = Plugin.Config.CustomCommand;
            }
        }
    }

    private void DrawWindowSection() {
        using (ImGuiGroupPanel.BeginGroupPanel("Window")) {
            var openOnStartup = Plugin.Config.OpenOnStartup;
            if (ImGui.Checkbox(Language.SettingsWindowOpenOnStartup, ref openOnStartup)) {
                Plugin.Config.OpenOnStartup = openOnStartup;
                Plugin.Config.Save();
            }

            var openOnLogin = Plugin.Config.OpenOnLogin;
            if (ImGui.Checkbox(Language.SettingsWindowOpenLogin, ref openOnLogin)) {
                Plugin.Config.OpenOnLogin = openOnLogin;
                Plugin.Config.Save();
            }

            var allowCloseWithEscape = Plugin.Config.AllowCloseWithEscape;
            if (ImGui.Checkbox(Language.SettingsWindowAllowCloseWithEscape, ref allowCloseWithEscape)) {
                Plugin.Config.AllowCloseWithEscape = allowCloseWithEscape;
                Plugin.Config.Save();
            }
        }
    }

    private void DrawDeveloperSection() {
        using (ImGuiGroupPanel.BeginGroupPanel("Developer")) {
            var debugMode = Plugin.Config.DebugMode;
            if (ImGui.Checkbox("Debug Mode##DebugMode", ref debugMode)) {
                Plugin.Config.DebugMode = debugMode;
                Plugin.Config.Save();
            }
            ImGuiUtil.HelpMarker("Enables the Simulate Roll button on all game windows.\nUseful for solo testing and learning the games.", sameline: true);
        }
    }

    private void DrawActionButtons() {
        using (ImRaii.PushColor(ImGuiCol.Button, Style.Components.ButtonPurpleNormal)
            .Push(ImGuiCol.ButtonHovered, Style.Components.ButtonPurpleHovered)
            .Push(ImGuiCol.ButtonActive, Style.Components.ButtonPurpleActive)) {
            if (ImGui.Button(Language.OpenPluginFolder))
                WindowsApi.OpenFolder(DalamudApi.PluginInterface.ConfigDirectory.FullName);

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(0, 20);
            ImGui.SameLine();

            if (ImGui.Button(Language.OpenPluginConfigFile))
                WindowsApi.OpenFile(DalamudApi.PluginInterface.ConfigFile.FullName);
        }
    }
}
