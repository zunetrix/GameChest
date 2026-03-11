namespace GameChest;

/// <summary>
/// Utility for normalizing FFXIV player names.
/// Names in chat arrive as "FirstName LastName" (without @World),
/// while registered names are stored as "FirstName LastName@World".
/// </summary>
public static class PlayerName {
    /// <summary>Strips the @World suffix, returning just "FirstName LastName".</summary>
    public static string Short(string fullName) {
        var i = fullName.IndexOf('@');
        return i >= 0 ? fullName[..i] : fullName;
    }

    /// <summary>
    /// Returns true if two player name strings refer to the same player,
    /// ignoring @World suffix and case.
    /// </summary>
    public static bool Matches(string a, string b) =>
        Short(a).Equals(Short(b), System.StringComparison.OrdinalIgnoreCase);
}
