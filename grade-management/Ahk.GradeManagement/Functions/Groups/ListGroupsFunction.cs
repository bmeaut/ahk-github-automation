using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.Groups
{
    public class ListGroupsFunction
    {
        private readonly ILogger _logger;

        public ListGroupsFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ListGroupsFunction>();
        }

        [Function("list-groups")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list-groups/{*subject}")] HttpRequestData req, string subject)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
