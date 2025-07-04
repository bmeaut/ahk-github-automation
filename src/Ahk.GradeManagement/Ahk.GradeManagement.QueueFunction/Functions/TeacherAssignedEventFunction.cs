using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.QueueFunction.Functions;

public class TeacherAssignedEventFunction(
    ILogger<TeacherAssignedEventFunction> logger,
    AssignmentEventProcessorService assignmentEventProcessorService)
{
    [Function(nameof(TeacherAssignedEventFunction))]
    public async Task Run(
        [QueueTrigger("ahkstatustracking-teacher-assigned", Connection = "AHK_EventsQueueConnectionString")]
        TeacherAssigned teacherAssigned)
    {
        logger.LogInformation(
            $"Teacher assigned event function triggered for repo url: {teacherAssigned.GitHubRepositoryUrl}");
        await assignmentEventProcessorService.ConsumeTeacherAssignedEventAsync(teacherAssigned);
        logger.LogInformation(
            $"Teacher assigned event consumed for repo url: {teacherAssigned.GitHubRepositoryUrl}");
    }
}
