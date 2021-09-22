using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data
{
    public interface IResultsRepository
    {
        Task AddResult(StudentResult value);
        Task<IReadOnlyCollection<StudentResult>> ListWithRepositoryPrefix(string repoPrefix);
    }
}