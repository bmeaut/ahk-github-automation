using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.Services.SetGradeService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.SetGrade
{
    public class SetGradeEventFunction
    {
        private readonly ISetGradeService service;
        private readonly ILogger logger;

        public SetGradeEventFunction(ISetGradeService service, ILogger logger)
        {
            this.service = service;
            this.logger = logger;
        }
            

        [Function("SetGradeEventFunction")]
        public async Task Run([QueueTrigger("ahksetgrade", Connection = "AHK_EventsQueueConnectionString")] SetGradeEvent data)
        {
            logger.LogInformation("SetGradeEventFunction triggered for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);

            if (string.IsNullOrEmpty(data.Neptun) || string.IsNullOrEmpty(data.Repository) || data.Results == null || data.Results.Length == 0)
            {
                logger.LogWarning("SetGradeEventFunction missing data for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);
                return;
            }

            try
            {
                await service.SetGradeAsync(data);
                logger.LogInformation("SetGradeEventFunction completed for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SetGradeEventFunction failed for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);
                throw;
            }
        }
    }
}
