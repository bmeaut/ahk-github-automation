using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[ApiController]
[Route("api/testassignmentevents")]
public class AssingmentEventTestController(AssignmentEventProcessorService assignmentEventProcessorService, IWebHostEnvironment environment)
    : ControllerBase
{
    [HttpPost("assignmentaccepted")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignmentAcceptedAsync(AssignmentAccepted assignmentAccepted)
    {
        if (!environment.IsDevelopment())
        {
            return BadRequest("This endpoint is only available in Development environment.");
        }

        await assignmentEventProcessorService.ConsumeAssignmentAcceptedEventAsync(assignmentAccepted);
        return Ok();
    }

    [HttpPost("pullrequestopened")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PullRequestOpenedAsync(PullRequestOpened pullRequestOpened)
    {
        if (!environment.IsDevelopment())
        {
            return BadRequest("This endpoint is only available in Development environment.");
        }

        await assignmentEventProcessorService.ConsumePullRequestOpenedEventAsync(pullRequestOpened);
        return Ok();
    }

    [HttpPost("cievaluationcompleted")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CiEvaluationCompletedAsync(CiEvaluationCompleted ciEvaluationCompleted)
    {
        if (!environment.IsDevelopment())
        {
            return BadRequest("This endpoint is only available in Development environment.");
        }

        await assignmentEventProcessorService.ConsumeCiEvaluationCompletedEventAsync(ciEvaluationCompleted);
        return Ok();
    }

    [HttpPost("teacherassigned")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TeacherAssignedAsync(TeacherAssigned teacherAssigned)
    {
        if (!environment.IsDevelopment())
        {
            return BadRequest("This endpoint is only available in Development environment.");
        }

        await assignmentEventProcessorService.ConsumeTeacherAssignedEventAsync(teacherAssigned);
        return Ok();
    }

    [HttpPost("assignmentgraded")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignmentGradedAsync(AssignmentGradedByTeacher assignmentGraded)
    {
        if (!environment.IsDevelopment())
        {
            return BadRequest("This endpoint is only available in Development environment.");
        }

        await assignmentEventProcessorService.ConsumeAssignmentGradedByTeacherEventAsync(assignmentGraded);
        return Ok();
    }

    [HttpPost("pullrequestclosed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PullRequestClosedAsync(PullRequestStatusChanged pullRequestStatusChanged)
    {
        if (!environment.IsDevelopment())
        {
            return BadRequest("This endpoint is only available in Development environment.");
        }

        await assignmentEventProcessorService.ConsumePullRequestStatusChangedEventAsync(pullRequestStatusChanged);
        return Ok();
    }
}
