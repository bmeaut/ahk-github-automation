using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Ahk.GradeManagement.ListGrades
{
    public class ListGradesFunction
    {
        private readonly IGradeListing service;
        private readonly ILogger logger;

        public ListGradesFunction(IGradeListing service, ILogger logger)
        {
            this.service = service;
            this.logger = logger;
        }

        [Function("list-grades")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "get", Route = "list-grades/{*repoprefix}")] HttpRequest req,
            string repoprefix)
        {
            logger.LogInformation($"Received request to list grades with prefix: {repoprefix}");

            var acceptHeader = req.Headers.GetValueOrDefault(HeaderNames.Accept);
            var results = await service.List(repoprefix);

            if (acceptHeader.Equals("text/csv", StringComparison.OrdinalIgnoreCase))
                return new FileContentResult(Encoding.UTF8.GetBytes(CsvExporter.GetCsv(results)), "text/csv; charset=utf-8");
            else
                return new OkObjectResult(results);
        }
    }
}
