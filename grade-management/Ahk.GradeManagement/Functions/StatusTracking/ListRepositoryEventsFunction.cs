using System.Net;
using System.Threading.Tasks;
using Ahk.GradeManagement.StatusTracking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.StatusTracking
{
    public class ListRepositoryEventsFunction
    {
        private readonly IStatusTrackingService service;
        private readonly ILogger logger;

        public ListRepositoryEventsFunction(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<ListRepositoryEventsFunction>();
        }

        [Function("ListRepositoryEventsFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "list-statuses/{*repoprefix}")] HttpRequestData req, string prefix)
        {
            logger.LogInformation($"Received request to list events for repo with prefix: {prefix}");

            var results = await service.ListEventsForRepository(prefix);
            return new OkObjectResult(results);

        }
    }
}
