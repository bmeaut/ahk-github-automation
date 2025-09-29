using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.QueueFunction.Functions;

public class AssignmentGradedByTeacherEventFunction(
    ILogger<AssignmentGradedByTeacherEventFunction> logger,
    AssignmentEventProcessorService assignmentEventProcessorService)
{
    [Function(nameof(AssignmentGradedByTeacherEventFunction))]
    public async Task Run([QueueTrigger("ahkstatustracking-assignment-graded-by-teacher", Connection = "ahk-queue-storage")] AssignmentGradedByTeacher assignmentGradedByTeacher)
    {
        logger.LogInformation("Assignment graded by teacher event function triggered for repo url: {RepoUrl}", assignmentGradedByTeacher.GitHubRepositoryUrl);
        await assignmentEventProcessorService.ConsumeAssignmentGradedByTeacherEventAsync(assignmentGradedByTeacher);
        logger.LogInformation("Assignment graded by teacher event consumed for repo url: {RepoUrl}", assignmentGradedByTeacher.GitHubRepositoryUrl);
    }
}
