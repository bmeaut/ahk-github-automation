using GradeManagement.Bll;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GradeManagement.QueueFunction.Functions;

public class TeacherAssignedEventFunction
{
    private readonly ILogger<TeacherAssignedEventFunction> _logger;
    private readonly AssignmentEventProcessorService _assignmentEventProcessorService;

    public TeacherAssignedEventFunction(ILogger<TeacherAssignedEventFunction> logger,
        AssignmentEventProcessorService assignmentEventProcessorService)
    {
        _logger = logger;
        _assignmentEventProcessorService = assignmentEventProcessorService;
    }

    [Function(nameof(TeacherAssignedEventFunction))]
    public async Task Run(
        [QueueTrigger("teacher-assigned", Connection = "AHK_EventsQueueConnectionString")]
        TeacherAssigned teacherAssigned)
    {
        _logger.LogInformation(
            $"Teacher assigned event function triggered for repo url: {teacherAssigned.GitHubRepositoryUrl}");
        await _assignmentEventProcessorService.ConsumeTeacherAssignedEventAsync(teacherAssigned);
        _logger.LogInformation(
            $"Teacher assigned event consumed for repo url: {teacherAssigned.GitHubRepositoryUrl}");
    }
}
