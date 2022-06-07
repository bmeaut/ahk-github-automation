using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ahk.GitHub.Monitor.Helpers
{
    internal class GradeCommentParser
    {
        private static readonly Regex CommandRegex = new Regex(@"^/ahk ok($|(\s.*))", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex GradesRegex = new Regex(@"[0-9]+([,\.][0-9]{1,3})?", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public GradeCommentParser(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                this.IsMatch = false;
                this.Grades = Array.Empty<double>();
                return;
            }

            var lines = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var m = CommandRegex.Match(line);
                if (m.Success)
                {
                    this.IsMatch = true;
                    this.Grades = getGrades(m.Value);
                }
            }
        }

        public bool IsMatch { get; }
        public IReadOnlyCollection<double> Grades { get; }
        public bool HasGrades => IsMatch && Grades.Count > 0;

        private static IReadOnlyCollection<double> getGrades(string value)
        {
            var gradesMatch = GradesRegex.Matches(value);
            return gradesMatch.Select(m => parseNum(m.Value)).ToArray();
        }

        private static double parseNum(string value)
        {
            // try parse as int or with decimal point double
            if (double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d1))
                return d1;

            // replace commas with decimal point
            if (double.TryParse(value.Replace(",", ".", StringComparison.OrdinalIgnoreCase), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d2))
                return d2;

            return double.NaN;
        }
    }
}
