using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GradeManagement.QueueFunction.Functions;

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
