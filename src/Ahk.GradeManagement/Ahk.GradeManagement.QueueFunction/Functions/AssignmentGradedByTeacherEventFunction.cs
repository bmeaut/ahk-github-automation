using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.QueueFunction.Functions;

public class AssignmentGradedByTeacherEventFunction
{
    private readonly ILogger<AssignmentGradedByTeacherEventFunction> _logger;
    private readonly AssignmentEventProcessorService _assignmentEventProcessorService;

    public AssignmentGradedByTeacherEventFunction(ILogger<AssignmentGradedByTeacherEventFunction> logger,
        AssignmentEventProcessorService assignmentEventProcessorService)
    {
        _logger = logger;
        _assignmentEventProcessorService = assignmentEventProcessorService;
    }

    [Function(nameof(AssignmentGradedByTeacherEventFunction))]
    public async Task Run(
        [QueueTrigger("ahkstatustracking-assignment-graded-by-teacher", Connection = "AHK_EventsQueueConnectionString")]
        AssignmentGradedByTeacher assignmentGradedByTeacher)
    {
        _logger.LogInformation(
            $"Assignment graded by teacher event function triggered for repo url: {assignmentGradedByTeacher.GitHubRepositoryUrl}");
        await _assignmentEventProcessorService.ConsumeAssignmentGradedByTeacherEventAsync(assignmentGradedByTeacher);
        _logger.LogInformation(
            $"Assignment graded by teacher event consumed for repo url: {assignmentGradedByTeacher.GitHubRepositoryUrl}");
    }
}
