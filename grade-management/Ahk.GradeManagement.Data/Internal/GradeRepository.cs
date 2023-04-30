using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data.Internal
{
    public class GradeRepository : IGradeRepository
    {
        public AhkDbContext Context { get; set; }
        public GradeRepository()
        {
        }

        public Task AddGrade(Grade value) => throw new NotImplementedException();
        public Task<Grade> GetLastResultOf(string neptun, string gitHubRepoName, int gitHubPrNumber) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<Grade>> ListConfirmedWithRepositoryPrefix(string repoPrefix) => throw new NotImplementedException();

        //public Task AddResult(Grade value) => base.Insert(value);
        //public Task<IReadOnlyCollection<Grade>> ListConfirmedWithRepositoryPrefix(string repoPrefix) => base.List(s => s.Confirmed && s.GithubRepoName.StartsWith(repoPrefix));

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Repo name is normalized to lowercase.")]
        //public Task<Grade> GetLastResultOf(string neptun, string gitHubRepoName, int gitHubPrNumber)
        //    => base.GetOneWithOrderByDescending(
        //        predicate: s => s.Student.Neptun == neptun.ToUpperInvariant() && s.GithubRepoName == gitHubRepoName.ToLowerInvariant() && s.GithubPrNumber == gitHubPrNumber,
        //        orderBy: s => s.Date);

    }
}
