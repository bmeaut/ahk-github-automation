using GradeManagement.Bll;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[ApiController]
[Route("api/testassignmentevents")]
public class AssingmentEventTestController(AssignmentEventProcessorService assignmentEventProcessorService)
    : ControllerBase
{
    [HttpPost("assignmentaccepted")]
    public async Task<IActionResult> AssignmentAcceptedAsync(AssignmentAccepted assignmentAccepted)
    {
        await assignmentEventProcessorService.ConsumeAssignmentAcceptedEventAsync(assignmentAccepted);
        return Ok();
    }

    [HttpPost("pullrequestopened")]
    public async Task<IActionResult> PullRequestOpenedAsync(PullRequestOpened pullRequestOpened)
    {
        await assignmentEventProcessorService.ConsumePullRequestOpenedEventAsync(pullRequestOpened);
        return Ok();
    }

    [HttpPost("cievaluationcompleted")]
    public async Task<IActionResult> CiEvaluationCompletedAsync(CiEvaluationCompleted ciEvaluationCompleted)
    {
        await assignmentEventProcessorService.ConsumeCiEvaluationCompletedEventAsync(ciEvaluationCompleted);
        return Ok();
    }

    [HttpPost("teacherassigned")]
    public async Task<IActionResult> TeacherAssignedAsync(TeacherAssigned teacherAssigned)
    {
        await assignmentEventProcessorService.ConsumeTeacherAssignedEventAsync(teacherAssigned);
        return Ok();
    }

    [HttpPost("assignmentgraded")]
    public async Task<IActionResult> AssignmentGradedAsync(AssignmentGradedByTeacher assignmentGraded)
    {
        await assignmentEventProcessorService.ConsumeAssignmentGradedByTeacherEventAsync(assignmentGraded);
        return Ok();
    }

    [HttpPost("pullrequestclosed")]
    public async Task<IActionResult> PullRequestClosedAsync(PullRequestStatusChanged pullRequestStatusChanged)
    {
        await assignmentEventProcessorService.ConsumePullRequestStatusChangedEventAsync(pullRequestStatusChanged);
        return Ok();
    }
}
