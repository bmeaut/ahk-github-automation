using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Functions.StatusTracking;
using Ahk.GradeManagement.Services.StatusTrackingService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.StatusTracking
{
    public class BranchCreateEventFunction
    {
        private readonly IStatusTrackingService service;
        private readonly ILogger logger;

        public BranchCreateEventFunction(IStatusTrackingService service, ILoggerFactory loggerFactory)
        {
            this.service = service;
            this.logger = loggerFactory.CreateLogger<BranchCreateEventFunction>();
        } 

        [Function("BranchCreateEventFunction")]
        public async Task Run([QueueTrigger("ahkstatustrackingbranchcreate", Connection = "AHK_EventsQueueConnectionString")] BranchCreateEvent data)
        {
            logger.LogInformation("BranchCreateEventFunction triggered for Repository='{Repository}', Branch='{Branch}'", data.Repository, data.Branch);

            if (string.IsNullOrEmpty(data.Branch) || string.IsNullOrEmpty(data.Repository))
            {
                logger.LogWarning("BranchCreateEventFunction missing data for Repository='{Repository}', Branch='{Branch}'", data.Repository, data.Branch);
                return;
            }

            try
            {
                await service.InsertNewEventAsync(data);
                logger.LogInformation("BranchCreateEventFunction completed for Repository='{Repository}', Branch='{Branch}'", data.Repository, data.Branch);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BranchCreateEventFunction failed for Repository='{Repository}', Branch='{Branch}'", data.Repository, data.Branch);
            }
        }
    }
}
