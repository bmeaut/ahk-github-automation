using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.StatusTracking
{
    public class RepositoryCreateEventFunction
    {
        private readonly IStatusTrackingService service;

        public RepositoryCreateEventFunction(IStatusTrackingService service) => this.service = service;

        [FunctionName("RepositoryCreateEventFunction")]
        [ExponentialBackoffRetry(5, "00:01:00", "00:05:00")]
        public async Task Run([QueueTrigger("ahkstatustrackingrepocreate", Connection = "AHK_EventsQueueConnectionString")] RepositoryCreateEvent data, ILogger log)
        {
            log.LogInformation("RepositoryCreateEventFunction triggered for Repository='{Repository}'", data.Repository);

            if (string.IsNullOrEmpty(data.Repository))
            {
                log.LogWarning("RepositoryCreateEventFunction missing data for Repository='{Repository}'", data.Repository);
                return;
            }

            try
            {
                await service.InsertNewEvent(data);
                log.LogInformation("RepositoryCreateEventFunction completed for Repository='{Repository}'", data.Repository);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "RepositoryCreateEventFunction failed for Repository='{Repository}'", data.Repository);
                throw;
            }
        }
    }
}
