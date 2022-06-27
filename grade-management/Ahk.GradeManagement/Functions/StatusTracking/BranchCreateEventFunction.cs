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
        public async Task Run([QueueTrigger("ahkstatustrackingbranchcreate", Connection = "AHK_EventsQueueConnectionString")] BranchCreateEvent data, ILogger log)
        {
            log.LogInformation("BranchCreateEventFunction triggered for Repository='{Repository}', Branch='{Branch}'", data.Repository, data.Branch);

            if (string.IsNullOrEmpty(data.Branch) || string.IsNullOrEmpty(data.Repository))
            {
                log.LogWarning("BranchCreateEventFunction missing data for Repository='{Repository}', Branch='{Branch}'", data.Repository, data.Branch);
                return;
            }

            try
            {
                await service.InsertNewEvent(data);
                log.LogInformation("BranchCreateEventFunction completed for Repository='{Repository}', Branch='{Branch}'", data.Repository, data.Branch);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "BranchCreateEventFunction failed for Repository='{Repository}', Branch='{Branch}'", data.Repository, data.Branch);
            }
        }
    }
}
