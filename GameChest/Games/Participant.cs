using System;

namespace GameChest;

public record Participant(string FullName, int RollResult, int RollOutOf, DateTime RolledAt);
