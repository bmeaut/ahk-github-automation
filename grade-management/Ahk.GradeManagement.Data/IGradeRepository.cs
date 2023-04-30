using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data
{
    public interface IGradeRepository
    {
        AhkDbContext Context { get; set; }
        Task AddGrade(Grade value);
        Task<Grade> GetLastResultOf(string neptun, string gitHubRepoName, int gitHubPrNumber);
        Task<IReadOnlyCollection<Grade>> ListConfirmedWithRepositoryPrefix(string repoPrefix);
    }
}
