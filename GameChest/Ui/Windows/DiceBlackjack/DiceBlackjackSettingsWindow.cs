using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using GameChest.Util.ImGuiExt;

namespace GameChest;

public class DiceBlackjackSettingsWindow : Window {
    private Plugin Plugin { get; }

    public DiceBlackjackSettingsWindow(Plugin plugin) : base("Dice Blackjack - Settings###DiceBlackjackSettingsWindow") {
        Plugin = plugin;
        Size = ImGuiHelpers.ScaledVector2(320, 240);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var cfg = Plugin.Config.DiceBlackjack;

        using (ImGuiGroupPanel.BeginGroupPanel("General")) {
            var outChannel = cfg.OutputChannel;
            if (OutputChannelCombo.Draw("##DbjOutput", ref outChannel, 180f * ImGuiHelpers.GlobalScale)) {
                cfg.OutputChannel = outChannel;
                Plugin.Config.Save();
            }

            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var maxRoll = cfg.MaxRoll;
            if (ImGui.InputInt("Max Roll##DbjMaxRoll", ref maxRoll, 1, 10)) {
                cfg.MaxRoll = Math.Clamp(maxRoll, 2, 9999);
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("The /random value used for each card draw.\nDefault 13 = standard blackjack deck (A, 2-10, J, Q, K).");

            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var target = cfg.TargetPoints;
            if (ImGui.InputInt("Target Points##DbjTarget", ref target, 1, 5)) {
                cfg.TargetPoints = Math.Clamp(target, 3, 9999);
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("Score limit — going over this busts the player. Default: 21.");

            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var dealerStand = cfg.DealerStandAt;
            if (ImGui.InputInt("Dealer Stand At##DbjDealerStand", ref dealerStand, 1, 5)) {
                cfg.DealerStandAt = Math.Clamp(dealerStand, 1, cfg.TargetPoints);
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("The dealer will keep drawing until reaching this total. Default: 17.");

            ImGui.SetNextItemWidth(80f * ImGuiHelpers.GlobalScale);
            var minPlayers = cfg.MinPlayers;
            if (ImGui.InputInt("Min Players##DbjMinPlayers", ref minPlayers, 1, 1)) {
                cfg.MinPlayers = Math.Clamp(minPlayers, 1, 50);
                Plugin.Config.Save();
            }

            var cardMapping = cfg.CardMapping;
            if (ImGui.Checkbox("Card Mapping##DbjCardMapping", ref cardMapping)) {
                cfg.CardMapping = cardMapping;
                Plugin.Config.Save();
            }
            ImGuiUtil.ToolTip("When enabled with Max Roll 13:\n  1 = Ace (1 or 11)\n  11 = Jack (10)\n  12 = Queen (10)\n  13 = King (10)");
        }
    }
}
