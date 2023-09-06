using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.Assignments
{
    public class ListAssignmentsFunction
    {
        private readonly ILogger _logger;

        public ListAssignmentsFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ListAssignmentsFunction>();
        }

        [Function("list-assignments")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list-assignments/{*subject}")] HttpRequestData req, string subject)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
