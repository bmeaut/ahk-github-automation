using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ahk.GitHub.Monitor.Helpers
{
    internal class GradeCommentParser
    {
        private static readonly Regex CommandRegex = new Regex(@"^/ahk ok($|(\s.*))",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex GradesRegex = new Regex(@"[0-9]+([,\.][0-9]{1,3})?",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public GradeCommentParser(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                this.IsMatch = false;
                this.GradesWithOrder = [];
                return;
            }

            var lines = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var m = CommandRegex.Match(line);
                if (m.Success)
                {
                    this.IsMatch = true;
                    this.GradesWithOrder = getGrades(m.Value);
                }
            }
        }

        public bool IsMatch { get; }
        public Dictionary<int, double> GradesWithOrder { get; }
        public bool HasGrades => this.IsMatch && this.GradesWithOrder.Count > 0;

        private static Dictionary<int, double> getGrades(string value)
        {
            var gradesMatch = GradesRegex.Matches(value);
            var gradeList = gradesMatch.Select(m => parseNum(m.Value)).ToArray();
            var grades = new Dictionary<int, double>();
            for (var i = 0; i < gradeList.Length; i++)
            {
                grades.Add(i + 1, gradeList[i]);
            }

            return grades;
        }

        private static double parseNum(string value)
        {
            // try parse as int or with decimal point double
            if (double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d1))
                return d1;

            // replace commas with decimal point
            if (double.TryParse(value.Replace(",", ".", StringComparison.OrdinalIgnoreCase),
                    NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d2))
                return d2;

            return double.NaN;
        }
    }
}
