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

            SubmissionInfoDTO results = mapper.Map<SubmissionInfoDTO>(await service.ListStatusForRepositoriesAsync(repoprefix)); 
            return new OkObjectResult(results);
        }
    }
}
