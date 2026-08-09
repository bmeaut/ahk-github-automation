using System.Globalization;
using System.Text;
using Ahk.Web.Services.Grading.Dto;

namespace Ahk.Web.Services.Grading;

/// <summary>
/// Semicolon-separated grade export. Ported verbatim from
/// <c>grade-management/.../ListGrades/CsvExporter.cs</c> — the column layout (Neptun;GitHubRepo;GitHubPr then
/// one column per distinct exercise name, sorted) and the "0.##" invariant number format are relied on by
/// downstream administration, so they must not drift.
/// </summary>
public static class CsvExporter
{
    public static string GetCsv(IReadOnlyCollection<FinalStudentGrade> results)
    {
        var exNames = results.SelectMany(r => r.Points.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        var str = new StringBuilder();

        var values = new List<string> { "Neptun", "GitHubRepo", "GitHubPr" };
        values.AddRange(exNames);
        str.AppendLine(FormatLine(values));

        foreach (var r in results)
        {
            values.Clear();
            values.Add(r.Neptun.ToUpperInvariant());
            values.Add(r.Repo);
            values.Add(r.PrUrl ?? string.Empty);

            foreach (var exName in exNames)
                values.Add(r.Points.TryGetValue(exName, out var p) ? p.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty);

            str.AppendLine(FormatLine(values));
        }

        return str.ToString();
    }

    private static string FormatLine(IReadOnlyCollection<string> values)
    {
        if (values is null || values.Count == 0)
            return string.Empty;

        var valuesString = values.Select(s => s is null ? string.Empty : s.Replace("\"", string.Empty, StringComparison.OrdinalIgnoreCase));
        return string.Join(";", valuesString);
    }
}
