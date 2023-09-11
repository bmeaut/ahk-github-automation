using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Ahk.GradeManagement.Services;
using Ahk.GradeManagement.Services.GroupService;
using AutoMapper;
using DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.Groups
{
    public class ListGroupsFunction
    {
        private readonly ILogger _logger;
        private readonly IGroupService groupService;
        private readonly Mapper mapper;

        public ListGroupsFunction(ILoggerFactory loggerFactory, IGroupService groupService, Mapper mapper)
        {
            _logger = loggerFactory.CreateLogger<ListGroupsFunction>();
            this.groupService = groupService;
            this.mapper = mapper;
        }

        [Function("list-groups")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list-groups/{subject}")] HttpRequest req, string subject)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var results = mapper.Map<List<GroupDTO>>(await groupService.ListGroupsAsync(subject));

            return new OkObjectResult(results);
        }
    }
}
