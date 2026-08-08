using System.Globalization;
using System.Text.RegularExpressions;

namespace Ahk.Web.Services.GitHubWebhooks;

/// <summary>
/// Parses the teacher's <c>/ahk ok</c> chatops command out of a comment body. Ported verbatim from
/// <c>github-monitor/.../Helpers/GradeCommentParser.cs</c>.
///
/// <para><c>/ahk ok</c> alone confirms the automated evaluation; <c>/ahk ok 5 3.5 0</c> overrides it, the
/// numbers mapping positionally onto exercises. Both comma and dot are accepted as the decimal separator,
/// because Hungarian keyboards produce the comma.</para>
///
/// <para>Two behaviours that look like bugs and are not: the loop does <em>not</em> break, so in a multi-line
/// comment the <em>last</em> matching line wins; and an unparseable number becomes <see cref="double.NaN"/>
/// rather than throwing.</para>
/// </summary>
internal sealed partial class GradeCommentParser
{
    [GeneratedRegex(@"^/ahk ok($|(\s.*))", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CommandRegex();

    [GeneratedRegex(@"[0-9]+([,\.][0-9]{1,3})?", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GradesRegex();

    public GradeCommentParser(string? value)
    {
        this.Grades = Array.Empty<double>();

        if (string.IsNullOrEmpty(value))
            return;

        var lines = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var m = CommandRegex().Match(line);
            if (m.Success)
            {
                this.IsMatch = true;
                this.Grades = GetGrades(m.Value);
            }
        }
    }

    public bool IsMatch { get; }

    public IReadOnlyList<double> Grades { get; }

    public bool HasGrades => IsMatch && Grades.Count > 0;

    private static IReadOnlyList<double> GetGrades(string value)
        => GradesRegex().Matches(value).Select(m => ParseNum(m.Value)).ToArray();

    private static double ParseNum(string value)
    {
        // Try to parse as an int, or as a double with a decimal point.
        if (double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d1))
            return d1;

        // Replace commas with a decimal point.
        if (double.TryParse(value.Replace(",", ".", StringComparison.OrdinalIgnoreCase), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d2))
            return d2;

        return double.NaN;
    }
}
