using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GameChest;

public static class PhraseTemplateRenderer {
    private static readonly Regex VarRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);
    private static readonly Regex ExtraSpaces = new(@" {2,}", RegexOptions.Compiled);

    public static string Render(string template, Dictionary<string, string> vars) {
        var result = VarRegex.Replace(template, match => {
            var key = match.Groups[1].Value;
            return vars.TryGetValue(key, out var val) ? val : match.Value;
        });
        return ExtraSpaces.Replace(result, " ").Trim();
    }
}
