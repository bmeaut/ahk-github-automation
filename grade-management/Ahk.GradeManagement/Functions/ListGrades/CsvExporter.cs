using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ahk.GradeManagement.ListGrades
{
    internal static class CsvExporter
    {
        public static string GetCsv(IReadOnlyCollection<FinalStudentGrade> results)
        {
            var exNames = results.SelectMany(r => r.Points.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();

            var str = new StringBuilder();

            var values = new List<string>() { "Neptun", "GitHubRepo", "GitHubPr" };
            values.AddRange(exNames);

            str.AppendLine(formatLine(values));

            foreach (var r in results)
            {
                values.Clear();

                values.Add(r.Neptun.ToUpperInvariant());
                values.Add(r.Repo);
                values.Add(r.PrUrl);
                foreach (var exName in exNames)
                {
                    if (r.Points.TryGetValue(exName, out var p))
                        values.Add(p.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
                    else
                        values.Add(string.Empty);
                }

                str.AppendLine(formatLine(values));
            }

            return str.ToString();
        }

        private static string formatLine(IReadOnlyCollection<string> values)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            var valuesString = values.Select(s => s is null ? string.Empty : $"{s.Replace("\"", string.Empty, StringComparison.OrdinalIgnoreCase)}");
            return string.Join(";", valuesString);
        }
    }
}
