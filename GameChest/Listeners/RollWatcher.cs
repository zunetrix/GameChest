using System;

using Dalamud.Hooking;
using Dalamud.Memory;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;
using FFXIVClientStructs.STD;

namespace GameChest;

public unsafe class RollWatcher : IDisposable {
    private readonly Plugin Plugin;

    private static class Signatures {
        internal const string RandomPrintLog = "E8 ?? ?? ?? ?? EB ?? 45 33 C9 4C 8B C6";
        // internal const string DicePrintLog = "48 89 5C 24 ?? 48 89 74 24 ?? 55 57 41 54 41 55 41 56 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 0F B7 75"; // 7.4
        internal const string DicePrintLog = "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B7 BC 24";
        // internal const string DicePrintLog = "48 89 6C 24 ?? 56 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B7 BC 24";
    }

    private delegate void RandomPrintLogDelegate(RaptureLogModule* module, int logMessageId, byte* playerName, byte sex, StdDeque<TextParameter>* parameter, byte flags, ushort homeWorldId);
    private delegate void DicePrintLogDelegate(RaptureLogModule* module, ushort chatType, byte* userName, void* unused, ushort worldId, ulong accountId, ulong contentId, ushort roll, ushort outOf, uint entityId, byte ident);

    private readonly Hook<RandomPrintLogDelegate> RandomPrintLogHook;
    private readonly Hook<DicePrintLogDelegate> DicePrintLogHook;

    public RollWatcher(Plugin plugin) {
        Plugin = plugin;

        RandomPrintLogHook = DalamudApi.GameInteropProvider.HookFromSignature<RandomPrintLogDelegate>(Signatures.RandomPrintLog, RandomPrintLogDetour);
        DicePrintLogHook = DalamudApi.GameInteropProvider.HookFromSignature<DicePrintLogDelegate>(Signatures.DicePrintLog, DicePrintLogDetour);

        RandomPrintLogHook.Enable();
        DicePrintLogHook.Enable();
    }

    public void Dispose() {
        RandomPrintLogHook.Dispose();
        DicePrintLogHook.Dispose();
    }

    private void RandomPrintLogDetour(RaptureLogModule* module, int logMessageId, byte* playerName, byte sex, StdDeque<TextParameter>* parameter, byte flags, ushort homeWorldId) {
        if (logMessageId != 856 && logMessageId != 3887) {
            RandomPrintLogHook.Original(module, logMessageId, playerName, sex, parameter, flags, homeWorldId);
            return;
        }

        try {
            var name = MemoryHelper.ReadStringNullTerminated((nint)playerName);
            var world = WorldHelper.GetWorld(homeWorldId);
            // var fullName = $"{name}\uE05D{world.Value.Name}";
            var fullName = $"{name}@{world.Value.Name}";

            var roll = (*parameter)[1].IntValue;
            var outOf = logMessageId == 3887 ? (*parameter)[2].IntValue : 0;

            Plugin.RollManager.ProcessIncomingRollMessage(fullName, roll, outOf);
        } catch (Exception ex) {
            DalamudApi.PluginLog.Error(ex, "Unable to /random dice roll");
        }

        RandomPrintLogHook.Original(module, logMessageId, playerName, sex, parameter, flags, homeWorldId);
    }

    private void DicePrintLogDetour(RaptureLogModule* module, ushort chatType, byte* playerName, void* unused, ushort worldId, ulong accountId, ulong contentId, ushort roll, ushort outOf, uint entityId, byte ident) {
        try {
            var name = MemoryHelper.ReadStringNullTerminated((nint)playerName);
            var world = WorldHelper.GetWorld(worldId);
            var fullName = $"{name}@{world.Value.Name}";

            Plugin.RollManager.ProcessIncomingRollMessage(fullName, roll, outOf);
        } catch (Exception ex) {
            DalamudApi.PluginLog.Error(ex, "Unable to process /dice roll");
        }

        DicePrintLogHook.Original(module, chatType, playerName, unused, worldId, accountId, contentId, roll, outOf, entityId, ident);
    }
}
