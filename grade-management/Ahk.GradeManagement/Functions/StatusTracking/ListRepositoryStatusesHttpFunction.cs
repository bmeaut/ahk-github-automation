using System.Linq;
using System.Threading.Tasks;
using Ahk.GradeManagement.Functions.StatusTracking;
using Ahk.GradeManagement.Services.StatusTrackingService;
using AutoMapper;
using DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.StatusTracking
{
    public class ListRepositoryStatusesHttpFunction
    {
        private readonly IStatusTrackingService service;
        private readonly ILogger logger;
        private Mapper mapper;

        public ListRepositoryStatusesHttpFunction(IStatusTrackingService service, ILoggerFactory loggerFactory, Mapper mapper)
        {
            this.service = service;
            this.logger = loggerFactory.CreateLogger<ListRepositoryStatusesHttpFunction>();
            this.mapper = mapper;
        }

        [Function("list-statuses")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "get", Route = "list-statuses/{*repoprefix}")] HttpRequest req,
            string repoprefix)
        {
            logger.LogInformation($"Received request to list statuses with prefix: {repoprefix}");

            var infos = await service.ListStatusForRepositoriesAsync(repoprefix);

            var results = infos.Select(info => mapper.Map<SubmissionInfoDTO>(info)).ToList();
            return new OkObjectResult(results);
        }
    }
}
