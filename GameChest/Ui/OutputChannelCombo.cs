using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;

namespace GameChest;

internal static class OutputChannelCombo {
    private static readonly (XivChatType Type, string Label)[] Channels = {
        (XivChatType.Say,             "Say (/s)"),
        (XivChatType.Yell,            "Yell (/y)"),
        (XivChatType.Shout,           "Shout (/sh)"),
        (XivChatType.Party,           "Party (/p)"),
        (XivChatType.Echo,           "Echo (/echo)"),
        (XivChatType.FreeCompany,     "Free Company (/fc)"),
        (XivChatType.Ls1,             "Linkshell 1 (/l1)"),
        (XivChatType.Ls2,             "Linkshell 2 (/l2)"),
        (XivChatType.Ls3,             "Linkshell 3 (/l3)"),
        (XivChatType.Ls4,             "Linkshell 4 (/l4)"),
        (XivChatType.Ls5,             "Linkshell 5 (/l5)"),
        (XivChatType.Ls6,             "Linkshell 6 (/l6)"),
        (XivChatType.Ls7,             "Linkshell 7 (/l7)"),
        (XivChatType.Ls8,             "Linkshell 8 (/l8)"),
        (XivChatType.CrossLinkShell1, "Cross Linkshell 1 (/cwl1)"),
        (XivChatType.CrossLinkShell2, "Cross Linkshell 2 (/cwl2)"),
        (XivChatType.CrossLinkShell3, "Cross Linkshell 3 (/cwl3)"),
        (XivChatType.CrossLinkShell4, "Cross Linkshell 4 (/cwl4)"),
        (XivChatType.CrossLinkShell5, "Cross Linkshell 5 (/cwl5)"),
        (XivChatType.CrossLinkShell6, "Cross Linkshell 6 (/cwl6)"),
        (XivChatType.CrossLinkShell7, "Cross Linkshell 7 (/cwl7)"),
        (XivChatType.CrossLinkShell8, "Cross Linkshell 8 (/cwl8)"),
    };

    // Returns true when the value changed. Pass the current value; the ref is updated on change.
    public static bool Draw(string id, ref XivChatType current, float width = 200f) {
        var changed = false;
        ImGui.SetNextItemWidth(width);
        if (ImGui.BeginCombo(id, LabelOf(current))) {
            foreach (var (type, label) in Channels) {
                if (ImGui.Selectable(label, current == type)) {
                    current = type;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        return changed;
    }

    private static string LabelOf(XivChatType type) {
        foreach (var (t, l) in Channels)
            if (t == type) return l;
        return type.ToString();
    }
}
