using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Ahk.GradeManagement.Services.AssignmentService;
using AutoMapper;
using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.Assignments
{
    public class ListExercisesForAssignmentFunction
    {
        private readonly ILogger _logger;
        private readonly IAssignmentService assignmentService;
        private readonly Mapper mapper;

        public ListExercisesForAssignmentFunction(ILoggerFactory loggerFactory, IAssignmentService assignmentService, Mapper mapper)
        {
            _logger = loggerFactory.CreateLogger<ListExercisesForAssignmentFunction>();
            this.assignmentService = assignmentService;
            this.mapper = mapper;
        }

        [Function("list-exercises")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list-exercises/{subject}/{assignmentId}")] HttpRequestData req, string subject, string assignmentId)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var results = mapper.Map<List<ExerciseDTO>>(await assignmentService.ListExercisesAsync(subject, assignmentId));

            return new OkObjectResult(results);
        }
    }
}
