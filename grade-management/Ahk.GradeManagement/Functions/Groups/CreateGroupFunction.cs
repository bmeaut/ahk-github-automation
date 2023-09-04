using System;
using System.Net;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.ResultProcessing.Dto;
using Ahk.GradeManagement.Services;
using Ahk.GradeManagement.Services.GroupService;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Functions.Groups
{
    public class CreateGroupFunction
    {
        private readonly ILogger logger;
        private readonly IGroupService service;

        public CreateGroupFunction(ILoggerFactory loggerFactory, IGroupService _service)
        {
            logger = loggerFactory.CreateLogger<CreateGroupFunction>();
            service = _service;
        }

        [Function("CreateGroupFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "create-group")] HttpRequestData request)
        {
            logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await HttpRequestDataExtensions.ReadAsStringAsync(request);

            if (!PayloadReader.TryGetPayload<Group>(requestBody, out var requestDeserialized, out var deserializationError))
                return new BadRequestObjectResult(new { error = deserializationError });

            return await runCore(logger, requestDeserialized);
        }

        private async Task<IActionResult> runCore(ILogger logger, Group requestDeserialized)
        {
            try
            {
                await service.SaveGroupAsync(requestDeserialized);
                return new OkResult();
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { error = ex.ToString() }) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
