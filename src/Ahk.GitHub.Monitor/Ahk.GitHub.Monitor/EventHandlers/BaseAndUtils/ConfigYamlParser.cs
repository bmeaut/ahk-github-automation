using System;
using System.Text.RegularExpressions;

namespace Ahk.GitHub.Monitor.EventHandlers.BaseAndUtils;

internal static class ConfigYamlParser
{
    private static readonly Regex EnabledRegex = new(
        @"^enabled:?\s*(?<value>\w+)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

    public static bool IsEnabled(string fileContent)
    {
        // missing file -> disabled
        if (string.IsNullOrEmpty(fileContent))
        {
            return false;
        }

        // file content does not match -> disabled
        Match m = EnabledRegex.Match(fileContent);
        if (!m.Success)
        {
            return false;
        }

        Group value = m.Groups[@"value"];

        // no "true" or other part, just "enabled" -> ok
        if (!value.Success)
        {
            return true;
        }

        // "enabled: ..." must match either of the following
        return value.Value.Equals("true", StringComparison.OrdinalIgnoreCase)
               || value.Value.Equals("yes", StringComparison.OrdinalIgnoreCase)
               || value.Value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }
}
