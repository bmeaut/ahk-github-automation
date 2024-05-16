using GradeManagement.Bll;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GradeManagement.QueueFunction.Functions;

public class AssignmentAcceptedEventFunction
{
    private readonly ILogger<AssignmentAcceptedEventFunction> _logger;
    private readonly AssignmentEventProcessorService _assignmentEventProcessorService;

    public AssignmentAcceptedEventFunction(ILogger<AssignmentAcceptedEventFunction> logger,
        AssignmentEventProcessorService assignmentEventProcessorService)
    {
        _logger = logger;
        _assignmentEventProcessorService = assignmentEventProcessorService;
    }

    [Function(nameof(AssignmentAcceptedEventFunction))]
    public async Task Run(
        [QueueTrigger("assingment-accepted", Connection = "AHK_EventsQueueConnectionString")]
        AssignmentAccepted assignmentAcceptedEvent)
    {
        _logger.LogInformation(
            $"Assignment accepted event function triggered for repo url: {assignmentAcceptedEvent.GitHubRepositoryUrl}");
        await _assignmentEventProcessorService.ConsumeAssignmentAcceptedEventAsync(assignmentAcceptedEvent);
        _logger.LogInformation(
            $"Assignment accepted event consumed for repo url: {assignmentAcceptedEvent.GitHubRepositoryUrl}");
    }
}
