using GradeManagement.Bll;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[ApiController]
[Route("api/testassignmentevents")]
public class AssingmentEventTestController(AssingmentEventConsumerService assingmentEventConsumerService)
    : ControllerBase
{
    [HttpPost("assignmentaccepted")]
    public async Task<IActionResult> AssignmentAcceptedAsync(AssignmentAccepted assignmentAccepted)
    {
        await assingmentEventConsumerService.ConsumeAssignmentAcceptedEventAsync(assignmentAccepted);
        return Ok();
    }

    [HttpPost("pullrequestopened")]
    public async Task<IActionResult> PullRequestOpenedAsync(PullRequestOpened pullRequestOpened)
    {
        await assingmentEventConsumerService.ConsumePullRequestOpenedEvent(pullRequestOpened);
        return Ok();
    }

    [HttpPost("cievaluationcompleted")]
    public async Task<IActionResult> CiEvaluationCompletedAsync(CiEvaluationCompleted ciEvaluationCompleted)
    {
        await assingmentEventConsumerService.ConsumeCiEvaluationCompletedEvent(ciEvaluationCompleted);
        return Ok();
    }

    [HttpPost("teacherassigned")]
    public async Task<IActionResult> TeacherAssignedAsync(TeacherAssigned teacherAssigned)
    {
        await assingmentEventConsumerService.ConsumeTeacherAssignedEvent(teacherAssigned);
        return Ok();
    }

    [HttpPost("assignmentgraded")]
    public async Task<IActionResult> AssignmentGradedAsync(AssignmentGradedByTeacher assignmentGraded)
    {
        await assingmentEventConsumerService.ConsumeAssignmentGradedByTeacherEvent(assignmentGraded);
        return Ok();
    }

    [HttpPost("pullrequestclosed")]
    public async Task<IActionResult> PullRequestClosedAsync(PullRequestClosed pullRequestClosed)
    {
        await assingmentEventConsumerService.ConsumePullRequestClosedEvent(pullRequestClosed);
        return Ok();
    }
}
