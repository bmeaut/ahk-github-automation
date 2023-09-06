using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ahk.GradeManagement.Services.StatusTrackingService;
using AutoMapper;
using DTOs;
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
        private Mapper mapper;

        public ListRepositoryEventsFunction(IStatusTrackingService service, ILoggerFactory loggerFactory, Mapper mapper)
        {
            this.service = service;
            logger = loggerFactory.CreateLogger<ListRepositoryEventsFunction>();
            this.mapper = mapper;
        }

        [Function("list-events")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list-events/{*prefix}")] HttpRequestData req, string prefix)
        {
            logger.LogInformation($"Received request to list events for repo with prefix: {prefix}");

            var events = await service.ListEventsForRepositoryAsync(prefix);

            var results = events.Select(e => mapper.Map<StatusEventBaseDTO>(e)).ToList();
            return new OkObjectResult(results);

        }
    }
}
