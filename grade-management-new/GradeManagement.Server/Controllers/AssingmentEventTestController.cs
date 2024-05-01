using GradeManagement.Bll;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[ApiController]
[Route("api/testassignmentevents")]
public class AssingmentEventTestController(AssingmentEventProcessorService assingmentEventProcessorService)
    : ControllerBase
{
    [HttpPost("assignmentaccepted")]
    public async Task<IActionResult> AssignmentAcceptedAsync(AssignmentAccepted assignmentAccepted)
    {
        await assingmentEventProcessorService.ConsumeAssignmentAcceptedEventAsync(assignmentAccepted);
        return Ok();
    }

    [HttpPost("pullrequestopened")]
    public async Task<IActionResult> PullRequestOpenedAsync(PullRequestOpened pullRequestOpened)
    {
        await assingmentEventProcessorService.ConsumePullRequestOpenedEvent(pullRequestOpened);
        return Ok();
    }

    [HttpPost("cievaluationcompleted")]
    public async Task<IActionResult> CiEvaluationCompletedAsync(CiEvaluationCompleted ciEvaluationCompleted)
    {
        await assingmentEventProcessorService.ConsumeCiEvaluationCompletedEvent(ciEvaluationCompleted);
        return Ok();
    }

    [HttpPost("teacherassigned")]
    public async Task<IActionResult> TeacherAssignedAsync(TeacherAssigned teacherAssigned)
    {
        await assingmentEventProcessorService.ConsumeTeacherAssignedEvent(teacherAssigned);
        return Ok();
    }

    [HttpPost("assignmentgraded")]
    public async Task<IActionResult> AssignmentGradedAsync(AssignmentGradedByTeacher assignmentGraded)
    {
        await assingmentEventProcessorService.ConsumeAssignmentGradedByTeacherEvent(assignmentGraded);
        return Ok();
    }

    [HttpPost("pullrequestclosed")]
    public async Task<IActionResult> PullRequestClosedAsync(PullRequestStatusChanged pullRequestStatusChanged)
    {
        await assingmentEventProcessorService.ConsumePullRequestStatusChangedEventAsync(pullRequestStatusChanged);
        return Ok();
    }
}
