using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.StatusTracking
{
    public class WorkflowRunEventFunction
    {
        private readonly IStatusTrackingService service;
        private readonly ILogger logger;
        public WorkflowRunEventFunction(IStatusTrackingService service, ILoggerFactory loggerFactory)
        {
            this.service = service;
            this.logger = loggerFactory.CreateLogger<WorkflowRunEventFunction>();
        }

        [Function("WorkflowRunEventFunction")]
        public async Task Run([QueueTrigger("ahkstatustrackingworkflowrun", Connection = "AHK_EventsQueueConnectionString")] WorkflowRunEvent data)
        {
            logger.LogInformation("WorkflowRunEventFunction triggered for Repository='{Repository}', Conclusion='{Conclusion}'", data.Repository, data.Conclusion);

            if (string.IsNullOrEmpty(data.Repository))
            {
                logger.LogWarning("WorkflowRunEventFunction missing data for Repository='{Repository}', Conclusion='{Conclusion}'", data.Repository, data.Conclusion);
                return;
            }

            try
            {
                await service.InsertNewEvent(data);
                logger.LogInformation("WorkflowRunEventFunction completed for Repository='{Repository}', Conclusion='{Conclusion}'", data.Repository, data.Conclusion);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WorkflowRunEventFunction failed for Repository='{Repository}', Conclusion='{Conclusion}'", data.Repository, data.Conclusion);
                throw;
            }
        }
    }
}
