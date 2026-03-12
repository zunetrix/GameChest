// Replaces Games/RollManager.cs in the GameChest2 (test) compilation.
// RollManager is a no-op; Roll is the same data class as production.
using System;

namespace GameChest;

public class RollManager {
    public void ProcessIncomingRollMessage(string fullName, int result, int outOf) { /* no-op */ }
}

public class Roll {
    public string PlayerName;
    public int Result;
    public int OutOf;
    public DateTime At { get; } = DateTime.Now;

    private Roll(string name) { PlayerName = name; }

    public Roll(string fullName, int roll, int outOf) {
        PlayerName = fullName;
        Result = roll;
        OutOf = outOf != 0 ? outOf : -1;
    }

    public static Roll Dummy(string name = "Unknown") => new(name);
}
