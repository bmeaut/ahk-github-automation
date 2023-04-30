using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Data.Internal;

namespace Ahk.Grademanagement.Data.Internal
{
    internal class StatusTrackingRepository : IStatusTrackingRepository
    {
        public StatusTrackingRepository()
        {
        }

        public Task InsertNewEvent(StatusEventBase data) => throw new System.NotImplementedException();
        public Task<IReadOnlyCollection<StatusEventBase>> ListEventsForRepositories(string prefix) => throw new System.NotImplementedException();
    }
}
