using System.Text.RegularExpressions;

namespace Ahk.Web.Services.GitHubWebhooks;

/// <summary>
/// Reads the <c>enabled</c> flag out of a repository's <c>.github/ahk-monitor.yml</c>. Ported verbatim from
/// <c>github-monitor</c>: this is the opt-in gate that keeps the portal from acting on every repository in an
/// organization, and its exact accepted spellings (<c>enabled</c>, <c>enabled: true</c>, <c>yes</c>, <c>1</c>)
/// are what existing course templates rely on.
/// </summary>
internal static partial class ConfigYamlParser
{
    [GeneratedRegex(@"^enabled:?\s*(?<value>\w+)?", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex EnabledRegex();

    public static bool IsEnabled(string? fileContent)
    {
        // Missing file -> disabled.
        if (string.IsNullOrEmpty(fileContent))
            return false;

        // File content does not match -> disabled.
        var m = EnabledRegex().Match(fileContent);
        if (!m.Success)
            return false;

        var value = m.Groups["value"];

        // No "true" or other part, just "enabled" -> ok.
        if (!value.Success)
            return true;

        return value.Value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Value.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || value.Value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }
}
