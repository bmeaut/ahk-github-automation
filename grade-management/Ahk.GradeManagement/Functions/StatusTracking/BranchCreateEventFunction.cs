using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.StatusTracking
{
    public class BranchCreateEventFunction
    {
        private readonly IStatusTrackingService service;

        public BranchCreateEventFunction(IStatusTrackingService service) => this.service = service;

        [FunctionName("BranchCreateEventFunction")]
        [ExponentialBackoffRetry(5, "00:01:00", "00:05:00")]
        public async Task Run([QueueTrigger("ahkstatustrackingbranchcreate", Connection = "AHK_EventsQueueConnectionString")] BranchCreateEvent data, ILogger log)
        {
            log.LogInformation("BranchCreateEventFunction triggered for Repository='{Repository}', Username='{Username}', Branch='{Branch}'", data.Repository, data.Username, data.Branch);

            if (string.IsNullOrEmpty(data.Branch) || string.IsNullOrEmpty(data.Repository))
            {
                log.LogWarning("BranchCreateEventFunction missing data for Repository='{Repository}' Branch='{Branch}'", data.Repository, data.Branch);
                return;
            }

            try
            {
                await service.InsertNewEvent(data);
                log.LogInformation("BranchCreateEventFunction completed for Repository='{Repository}', Username='{Username}', Branch='{Branch}'", data.Repository, data.Username, data.Branch);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "BranchCreateEventFunction failed for Repository='{Repository}', Username='{Username}', Branch='{Branch}'", data.Repository, data.Username, data.Branch);
            }
        }
    }
}
