using System.Collections.Generic;

namespace Ahk.GradeManagement.Data.Entities
{
    /// <summary>
    /// Factory for normalizing data when creating an entity. Should be value converter in EF, but CosmosDB provider does not yet support those.
    /// </summary>
    public partial class StudentResult
    {
        public static StudentResult Create(string neptun, string repository, int? prNum, string prUrl, string actor, string origin, ICollection<ExerciseWithPoint> points)
            => new StudentResult()
            {
                Id = System.Guid.NewGuid().ToString(),
                Date = System.DateTime.UtcNow,
                Neptun = neptun.ToUpperInvariant(),
                GitHubRepoName = repository.ToLowerInvariant(),
                GitHubPrNumber = prNum,
                GitHubPrUrl = prUrl,
                Actor = actor,
                Origin = origin,
                Points = points,
            };
    }
}
