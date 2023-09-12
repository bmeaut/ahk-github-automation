using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Ahk.GradeManagement.Services.AssignmentService;
using Ahk.GradeManagement.Services.GroupService;
using AutoMapper;
using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.Assignments
{
    public class ListAssignmentsFunction
    {
        private readonly ILogger _logger;
        private readonly IAssignmentService assignmentService;
        private readonly Mapper mapper;

        public ListAssignmentsFunction(ILoggerFactory loggerFactory, IAssignmentService assignmentService, Mapper mapper)
        {
            _logger = loggerFactory.CreateLogger<ListAssignmentsFunction>();
            this.assignmentService = assignmentService;
            this.mapper = mapper;
        }

        [Function("list-assignments")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list-assignments/{subject}")] HttpRequestData req, string subject)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var results = mapper.Map<List<AssignmentDTO>>(await assignmentService.ListAsync(subject));

            return new OkObjectResult(results);
        }
    }
}
