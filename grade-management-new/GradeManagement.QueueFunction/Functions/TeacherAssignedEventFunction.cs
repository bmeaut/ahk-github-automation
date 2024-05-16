using GradeManagement.Bll;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GradeManagement.QueueFunction.Functions;

public class TeacherAssignedEventEventFunction
{
    private readonly ILogger<TeacherAssignedEventEventFunction> _logger;
    private readonly AssignmentEventProcessorService _assignmentEventProcessorService;

    public TeacherAssignedEventEventFunction(ILogger<TeacherAssignedEventEventFunction> logger,
        AssignmentEventProcessorService assignmentEventProcessorService)
    {
        _logger = logger;
        _assignmentEventProcessorService = assignmentEventProcessorService;
    }

    [Function(nameof(TeacherAssignedEventEventFunction))]
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
