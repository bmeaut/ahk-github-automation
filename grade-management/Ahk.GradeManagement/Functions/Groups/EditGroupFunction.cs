using System.Net;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Services.GroupService;
using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace Ahk.GradeManagement.Functions.Groups
{
    public class EditGroupFunction
    {
        private readonly ILogger _logger;
        private readonly IGroupService groupService;

        public EditGroupFunction(ILoggerFactory loggerFactory, IGroupService groupService)
        {
            _logger = loggerFactory.CreateLogger<EditGroupFunction>();
            this.groupService = groupService;
        }

        [Function("EditGroupFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "edit-group/{subject}")] HttpRequestData req, string subject,
            [FromBody] GroupDTO groupDTO)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var update = new Group()
            {
                Id = groupDTO.Id,
                Name = groupDTO.Name,
                Time = groupDTO.Time,
                Room = groupDTO.Room,
                SubjectId = groupDTO.SubjectId,
            };

            await groupService.UpdateGroupAsync(update);

            return new OkResult();
        }
    }
}
