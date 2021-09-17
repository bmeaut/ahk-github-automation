using System;
using System.Collections.Generic;

namespace Ahk.GradeManagement.Data.Entities
{
    public partial class StudentResult
    {
        public string Id { get; set; }

        public string Neptun { get; set; }
        public string GitHubRepoName { get; set; }
        public int? GitHubPrNumber { get; set; }
        public string GitHubPrUrl { get; set; }

        public DateTime Date { get; set; }
        public string Actor { get; set; }
        public string Origin { get; set; }
        public ICollection<ExerciseWithPoint> Points { get; set; }
    }
}
