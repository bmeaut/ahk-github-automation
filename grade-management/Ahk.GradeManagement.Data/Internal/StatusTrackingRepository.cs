using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Microsoft.Azure.Cosmos;

namespace Ahk.GradeManagement.Data.Internal
{
    internal class StatusTrackingRepository : RepositoryBase<StatusEventBase>, IStatusTrackingRepository
    {
        private const string ContainerName = "events";

        public StatusTrackingRepository(CosmosClient client)
            : base(client, ContainerName, DefaultPartitionKey)
        {
        }

        public Task<IReadOnlyCollection<StatusEventBase>> ListEventsForRepositories(string prefix) => base.List(o => o.Repository.StartsWith(prefix));
        public Task InsertNewEvent(StatusEventBase data) => base.Insert(data, data.Id);
    }
}
