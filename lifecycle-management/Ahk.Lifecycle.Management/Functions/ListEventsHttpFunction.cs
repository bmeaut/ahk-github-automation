using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ahk.Lifecycle.Management.DAL;

namespace Ahk.Lifecycle.Management
{
    public class ListEventsHttpFunction
    {
        private readonly IRepository service;

        public ListEventsHttpFunction(IRepository service) => this.service = service;

        [FunctionName("ListEventsHttpFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ListEventsHttpFunction/{*prefix}")] HttpRequest req,
            ILogger log, string prefix)
        {
            log.LogInformation("ListEventsHttpFunction triggered");

            var results = await service.GetRepositories(prefix);

            return new OkObjectResult(results);
        }
    }
}
