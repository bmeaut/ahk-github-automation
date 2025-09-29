using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.QueueFunction.Functions;

public class AssignmentAcceptedEventFunction(
    ILogger<AssignmentAcceptedEventFunction> logger,
    AssignmentEventProcessorService assignmentEventProcessorService)
{
    [Function(nameof(AssignmentAcceptedEventFunction))]
    public async Task Run([QueueTrigger("ahkstatustracking-assignment-accepted", Connection = "ahk-queue-storage")] AssignmentAccepted assignmentAcceptedEvent)
    {
        logger.LogInformation("Assignment accepted event function triggered for repo url: {RepoUrl}", assignmentAcceptedEvent.GitHubRepositoryUrl);
        await assignmentEventProcessorService.ConsumeAssignmentAcceptedEventAsync(assignmentAcceptedEvent);
        logger.LogInformation("Assignment accepted event consumed for repo url: {RepoUrl}", assignmentAcceptedEvent.GitHubRepositoryUrl);
    }
}
