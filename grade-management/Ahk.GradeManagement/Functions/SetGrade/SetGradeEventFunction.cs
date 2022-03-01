using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.SetGrade
{
    public class SetGradeEventFunction
    {
        private readonly ISetGradeService service;

        public SetGradeEventFunction(ISetGradeService service)
            => this.service = service;

        [FunctionName("SetGradeEventFunction")]
        [ExponentialBackoffRetry(5, "00:01:00", "00:05:00")]
        public async Task Run([QueueTrigger("ahksetgrade", Connection = "AHK_EventsQueueConnectionString")] SetGradeEvent data, ILogger log)
        {
            log.LogInformation("SetGradeEventFunction triggered for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);

            if (string.IsNullOrEmpty(data.Neptun) || string.IsNullOrEmpty(data.Repository) || data.Results == null || data.Results.Length == 0)
            {
                log.LogWarning("SetGradeEventFunction missing data for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);
                return;
            }

            try
            {
                await service.SetGrade(data);
                log.LogInformation("SetGradeEventFunction completed for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "SetGradeEventFunction failed for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);
            }
        }
    }
}
