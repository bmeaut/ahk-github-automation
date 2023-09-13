using System.Net;
using System.Threading.Tasks;
using Ahk.GradeManagement.Services.GroupService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.Groups
{
    public class DeleteGroupFunction
    {
        private readonly ILogger _logger;
        private readonly IGroupService groupService;

        public DeleteGroupFunction(ILoggerFactory loggerFactory, IGroupService groupService)
        {
            _logger = loggerFactory.CreateLogger<DeleteGroupFunction>();
            this.groupService = groupService;
        }

        [Function("delete-group")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "delete-group/{id}")] HttpRequestData req, int id)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            await groupService.DeleteGroupAsync(id);

            return new OkResult();
        }
    }
}
