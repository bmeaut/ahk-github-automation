using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Functions.StatusTracking;
using Ahk.GradeManagement.Services.StatusTrackingService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.StatusTracking
{
    public class PullRequestEventFunction
    {
        private readonly IStatusTrackingService service;
        private readonly ILogger logger;

        public PullRequestEventFunction(IStatusTrackingService service, ILoggerFactory loggerFactory)
        {
            this.service = service;
            this.logger = loggerFactory.CreateLogger<PullRequestEventFunction>();
        }

        [Function("PullRequestEventFunction")]
        public async Task Run([QueueTrigger("ahkstatustrackingpullrequest", Connection = "AHK_EventsQueueConnectionString")] PullRequestEvent data)
        {
            logger.LogInformation("PullRequestEventFunction triggered for Repository='{Repository}', Action='{Action}', Neptun='{Neptun}'", data.Repository, data.Action, data.Neptun);

            if (string.IsNullOrEmpty(data.Repository) || data.Number == 0)
            {
                logger.LogWarning("PullRequestEventFunction triggered for Repository='{Repository}', Action='{Action}', Neptun='{Neptun}'", data.Repository, data.Action, data.Neptun);
                return;
            }

            try
            {
                await service.InsertNewEventAsync(data);
                logger.LogInformation("PullRequestEventFunction completed for Repository='{Repository}', Action='{Action}', Neptun='{Neptun}'", data.Repository, data.Action, data.Neptun);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PullRequestEventFunction failed for Repository='{Repository}', Action='{Action}', Neptun='{Neptun}'", data.Repository, data.Action, data.Neptun);
                throw;
            }
        }
    }
}
