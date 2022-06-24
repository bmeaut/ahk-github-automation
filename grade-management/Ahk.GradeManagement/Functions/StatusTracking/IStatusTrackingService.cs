using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.StatusTracking
{
    public interface IStatusTrackingService
    {
        Task InsertNewEvent(StatusEventBase data);
        Task<IReadOnlyCollection<RepositoryStatus>> ListStatusForRepositories(string repoPrefix);
    }
}
