using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data
{
    public interface IStatusTrackingRepository
    {
        Task InsertNewEvent(StatusEventBase data);
        Task<IReadOnlyCollection<StatusEventBase>> ListEventsForRepositories(string prefix);
    }
}
