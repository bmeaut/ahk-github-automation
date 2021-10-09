using Ahk.GradeManagement.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data
{
    public interface IResultsRepository
    {
        Task AddResult(StudentResult value);
        Task<StudentResult> GetLastResultOf(string neptun, string gitHubRepoName, int gitHubPrNumber);
        Task<IReadOnlyCollection<StudentResult>> ListConfirmedWithRepositoryPrefix(string repoPrefix);
    }
}