using System.Text.RegularExpressions;

namespace PublishResult.Processing;

public partial class NeptunParser
{
    [GeneratedRegex("^[a-zA-Z0-9]{6}$", RegexOptions.Compiled)]
    private static partial Regex NeptunRegex();

    public static string? ParseNeptun(string neptunFileName)
    {
        using var file = File.OpenText(neptunFileName);
        var neptun = file.ReadToEnd();

        if (string.IsNullOrEmpty(neptun) || !NeptunRegex().IsMatch(neptun))
            return null;

        return neptun;
    }
}
