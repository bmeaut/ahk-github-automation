using System.Net;
using System.Threading.Tasks;
using Ahk.GradeManagement.Services.StatusTrackingService;
using Ahk.GradeManagement.Services.SubjectService;
using AutoMapper;
using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.Subjects
{
    public class GetSubjectsFunction
    {
        private readonly ILogger _logger;
        private readonly ISubjectService service;
        private Mapper mapper;

        public GetSubjectsFunction(ILoggerFactory loggerFactory, ISubjectService service, Mapper mapper)
        {
            _logger = loggerFactory.CreateLogger<GetSubjectsFunction>();
            this.service = service;
            this.mapper = mapper;
        }

        [Function("GetSubjectsFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list-subjects")] HttpRequestData req)
        {
            _logger.LogInformation($"Received request to list all subjects");

            var results = mapper.Map<SubjectDTO>(service.GetAllSubjects());

            return new OkObjectResult(results);
        }
    }
}
