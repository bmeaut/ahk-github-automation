using GradeManagement.Bll.Services;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GradeManagement.QueueFunction.Functions;

public class AssignmentAcceptedEventFunction(
    ILogger<AssignmentAcceptedEventFunction> logger,
    AssignmentEventProcessorService assignmentEventProcessorService)
{
    [Function(nameof(AssignmentAcceptedEventFunction))]
    public async Task Run(
        [QueueTrigger("ahkstatustracking-assignment-accepted", Connection = "AHK_EventsQueueConnectionString")]
        AssignmentAccepted assignmentAcceptedEvent)
    {
        logger.LogInformation(
            $"Assignment accepted event function triggered for repo url: {assignmentAcceptedEvent.GitHubRepositoryUrl}");
        await assignmentEventProcessorService.ConsumeAssignmentAcceptedEventAsync(assignmentAcceptedEvent);
        logger.LogInformation(
            $"Assignment accepted event consumed for repo url: {assignmentAcceptedEvent.GitHubRepositoryUrl}");
    }
}
