using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.StatusTracking
{
    public class WorkflowRunEventFunction
    {
        private readonly IStatusTrackingService service;

        public WorkflowRunEventFunction(IStatusTrackingService service) => this.service = service;

        [FunctionName("WorkflowRunEventFunction")]
        [ExponentialBackoffRetry(5, "00:01:00", "00:05:00")]
        public async Task Run([QueueTrigger("ahkstatustrackingworkflowrun", Connection = "AHK_EventsQueueConnectionString")] WorkflowRunEvent data, ILogger log)
        {
            log.LogInformation("WorkflowRunEventFunction triggered for Repository='{Repository}', Username='{Username}', Conclusion='{Conclusion}'", data.Repository, data.Username, data.Conclusion);

            if (string.IsNullOrEmpty(data.Repository))
            {
                log.LogWarning("WorkflowRunEventFunction missing data for Repository='{Repository}', Username='{Username}', Conclusion='{Conclusion}'", data.Repository, data.Username, data.Conclusion);
                return;
            }

            try
            {
                await service.InsertNewEvent(data);
                log.LogInformation("WorkflowRunEventFunction completed for Repository='{Repository}', Username='{Username}', Conclusion='{Conclusion}'", data.Repository, data.Username, data.Conclusion);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "WorkflowRunEventFunction failed for Repository='{Repository}', Username='{Username}', Conclusion='{Conclusion}'", data.Repository, data.Username, data.Conclusion);
                throw;
            }
        }
    }
}
