using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.SetGrade
{
    public class ConfirmAutoGradeEventFunction
    {
        private readonly ISetGradeService service;

        public ConfirmAutoGradeEventFunction(ISetGradeService service)
            => this.service = service;

        [FunctionName("ConfirmAutoGradeEventFunction")]
        [ExponentialBackoffRetry(5, "00:01:00", "00:05:00")]
        public async Task Run([QueueTrigger("ahkconfirmautograde", Connection = "AHK_EventsQueueConnectionString")] ConfirmAutoGradeEvent data, ILogger log)
        {
            log.LogInformation("ConfirmAutoGradeEventFunction triggered for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);

            if (string.IsNullOrEmpty(data.Neptun) || string.IsNullOrEmpty(data.Repository))
            {
                log.LogWarning("ConfirmAutoGradeEventFunction missing data for Neptun={Neptun} Repository={Repository} Pr={PullRequest}; event data is: " + getDataAsString(data), data.Neptun, data.Repository, data.PrNumber);
                return;
            }

            try
            {
                await service.ConfirmAutoGrade(data);
                log.LogInformation("ConfirmAutoGradeEventFunction completed for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "ConfirmAutoGradeEventFunction failed for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);
            }
        }

        private static string getDataAsString(ConfirmAutoGradeEvent data)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(data);
            }
            catch
            {
                return "N/A";
            }
        }
    }
}
