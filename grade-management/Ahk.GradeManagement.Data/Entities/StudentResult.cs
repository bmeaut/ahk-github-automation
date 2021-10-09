using System;
using System.Collections.Generic;

namespace Ahk.GradeManagement.Data.Entities
{
    public partial class StudentResult
    {
        public StudentResult(string id, string neptun, string gitHubRepoName, int? gitHubPrNumber, string gitHubPrUrl, DateTime date, string actor, string origin, ICollection<ExerciseWithPoint> points, bool confirmed)
        {
            this.Id = id ?? Guid.NewGuid().ToString();
            this.Date = date;
            this.Neptun = neptun.ToUpperInvariant();
            this.GitHubRepoName = gitHubRepoName.ToLowerInvariant();
            this.GitHubPrNumber = gitHubPrNumber;
            this.GitHubPrUrl = gitHubPrUrl;
            this.Actor = actor;
            this.Origin = origin;
            this.Points = points;
            this.Confirmed = confirmed;
        }

        public string Id { get; }

        public string Neptun { get; }
        public string GitHubRepoName { get; }
        public int? GitHubPrNumber { get; }
        public string GitHubPrUrl { get; }

        public DateTime Date { get; }
        public string Actor { get; }
        public string Origin { get; }
        public ICollection<ExerciseWithPoint> Points { get; }

        public bool Confirmed { get; }
    }
}
