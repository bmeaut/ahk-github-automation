using System.Collections.Generic;

namespace Ahk.GradeManagement.ListGrades
{
    public class FinalStudentGrade
    {
        public FinalStudentGrade(string assignmentName, string neptun, string repo, string prUrl, Dictionary<string, double> points)
        {
            AssignmentName = assignmentName;
            Neptun = neptun;
            Repo = repo;
            PrUrl = prUrl;
            Points = points;
        }

        public string AssignmentName { get; }
        public string Neptun { get; }
        public string Repo { get; }
        public string PrUrl { get; }
        public IReadOnlyDictionary<string, double> Points { get; }
    }
}
