using System.Collections.Generic;
using System.Linq;

using Lumina.Excel.Sheets;

namespace GameChest;

public static class MinionHelper {
    private static Minion GetMinion(Companion minion) {
        return new Minion {
            Id = minion.RowId,
            Name = minion.Singular.ToString(),
            Race = minion.MinionRace.Value.Name.ToString(),
            IconId = minion.Icon,
        };
    }

    public static List<Minion> GetAllowedItems() {
        return DalamudApi.DataManager.GetExcelSheet<Companion>()
            .Select(GetMinion)
            .ToList();
    }

    private static Companion? GetMinion(uint id) {
        return DalamudApi.DataManager.Excel.GetSheet<Companion>().GetRowOrDefault(id);
    }

    public static uint GetIconId(uint item) {
        uint undefinedIcon = 60042;
        return GetMinion(item)?.Icon ?? undefinedIcon;
    }
}

public class Minion {
    public uint Id;
    public uint IconId;
    public string Name;
    public string Race;
}
