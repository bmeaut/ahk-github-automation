using System;
using System.Net;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Services.AssignmentService;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.Assignments
{
    public class CreateAssignmentFunction
    {
        private readonly ILogger _logger;
        private readonly IAssignmentService service;

        public CreateAssignmentFunction(ILoggerFactory loggerFactory, IAssignmentService service)
        {
            _logger = loggerFactory.CreateLogger<CreateAssignmentFunction>();
            this.service = service;
        }

        [Function("create-assignment")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "create-assignment")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await HttpRequestDataExtensions.ReadAsStringAsync(req);

            if (!PayloadReader.TryGetPayload<Assignment>(requestBody, out var requestDeserialized, out var deserializationError))
                return new BadRequestObjectResult(new { error = deserializationError });


            return await runCore(_logger, requestDeserialized);
        }

        private async Task<IActionResult> runCore(ILogger logger, Assignment requestDeserialized)
        {
            try
            {
                await service.SaveAssignmentAsync(requestDeserialized);
                return new OkResult();
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { error = ex.ToString() }) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
