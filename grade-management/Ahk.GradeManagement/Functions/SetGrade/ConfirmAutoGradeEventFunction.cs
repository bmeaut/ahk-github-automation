using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.SetGrade
{
    public class ConfirmAutoGradeEventFunction
    {
        private readonly ISetGradeService service;
        private readonly ILogger logger;
        public ConfirmAutoGradeEventFunction(ISetGradeService service, ILogger logger)
        {
            this.service = service;
            this.logger = logger;
        }

        [Function("ConfirmAutoGradeEventFunction")]
        public async Task Run([QueueTrigger("ahkconfirmautograde", Connection = "AHK_EventsQueueConnectionString")] ConfirmAutoGradeEvent data)
        {
            logger.LogInformation("ConfirmAutoGradeEventFunction triggered for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);

            if (string.IsNullOrEmpty(data.Neptun) || string.IsNullOrEmpty(data.Repository))
            {
                logger.LogWarning("ConfirmAutoGradeEventFunction missing data for Neptun={Neptun} Repository={Repository} Pr={PullRequest}; event data is: " + getDataAsString(data), data.Neptun, data.Repository, data.PrNumber);
                return;
            }

            try
            {
                await service.ConfirmAutoGrade(data);
                logger.LogInformation("ConfirmAutoGradeEventFunction completed for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ConfirmAutoGradeEventFunction failed for Neptun={Neptun} Repository={Repository} Pr={PullRequest}", data.Neptun, data.Repository, data.PrNumber);
                throw;
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
