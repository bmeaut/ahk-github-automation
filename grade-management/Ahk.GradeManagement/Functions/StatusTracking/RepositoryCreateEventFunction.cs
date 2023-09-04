using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Services.StatusTrackingService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.StatusTracking
{
    public class RepositoryCreateEventFunction
    {
        private readonly IStatusTrackingService service;
        private readonly ILogger logger;

        public RepositoryCreateEventFunction(IStatusTrackingService service, ILoggerFactory loggerFactory)
        {
            this.service = service;
            this.logger = loggerFactory.CreateLogger<RepositoryCreateEventFunction>();
        }

        [Function("RepositoryCreateEventFunction")]
        public async Task Run([QueueTrigger("ahkstatustrackingrepocreate", Connection = "AHK_EventsQueueConnectionString")] RepositoryCreateEvent data)
        {
            logger.LogInformation("RepositoryCreateEventFunction triggered for Repository='{Repository}'", data.Repository);

            if (string.IsNullOrEmpty(data.Repository))
            {
                logger.LogWarning("RepositoryCreateEventFunction missing data for Repository='{Repository}'", data.Repository);
                return;
            }

            try
            {
                await service.InsertNewEventAsync(data);
                logger.LogInformation("RepositoryCreateEventFunction completed for Repository='{Repository}'", data.Repository);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RepositoryCreateEventFunction failed for Repository='{Repository}'", data.Repository);
                throw;
            }
        }
    }
}
