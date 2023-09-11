using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.Groups
{
    public class DeleteGroupFunction
    {
        private readonly ILogger _logger;

        public DeleteGroupFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DeleteGroupFunction>();
        }

        [Function("delete-group")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "delete-group/{subject}/{id}")] HttpRequestData req, string subject, int id)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
