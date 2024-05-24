﻿using GradeManagement.Bll.Services;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[ApiController]
[Route("api/cievaluation")]
public class CiEvaluationController(AssignmentEventProcessorService assignmentEventProcessorService) : ControllerBase
{
    [HttpPost("completed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CiEvaluationCompletedAsync(CiEvaluationCompleted ciEvaluationCompleted)
    {
        await assignmentEventProcessorService.ConsumeCiEvaluationCompletedEventAsync(ciEvaluationCompleted);
        return Ok();
    }
}
