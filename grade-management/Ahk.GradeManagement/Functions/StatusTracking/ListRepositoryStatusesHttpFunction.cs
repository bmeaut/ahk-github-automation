using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.StatusTracking
{
    public class ListRepositoryStatusesHttpFunction
    {
        private readonly IStatusTrackingService service;

        public ListRepositoryStatusesHttpFunction(IStatusTrackingService service) => this.service = service;

        [FunctionName("list-statuses")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "get", Route = "list-statuses/{*repoprefix}")] HttpRequest req,
            string repoprefix,
            ILogger logger)
        {
            logger.LogInformation($"Received request to list statuses with prefix: {repoprefix}");

            var results = await service.ListStatusForRepositories(repoprefix);
            return new OkObjectResult(results);
        }
    }
}
