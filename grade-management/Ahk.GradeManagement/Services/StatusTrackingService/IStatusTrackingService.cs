using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Data.Models;
using Ahk.GradeManagement.StatusTracking;

namespace Ahk.GradeManagement.Services.StatusTrackingService
{
    public interface IStatusTrackingService
    {
        Task InsertNewEventAsync(StatusEventBase data);
        Task<IReadOnlyCollection<SubmissionInfo>> ListStatusForRepositoriesAsync(string repoPrefix);
        Task<IReadOnlyCollection<StatusEventBase>> ListEventsForRepositoryAsync(string repo);
    }
}
