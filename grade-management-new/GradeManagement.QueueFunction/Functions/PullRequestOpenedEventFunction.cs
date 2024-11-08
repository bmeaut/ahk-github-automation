using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GradeManagement.QueueFunction.Functions;

public class PullRequestOpenedEventFunction(
    ILogger<PullRequestOpenedEventFunction> logger,
    AssignmentEventProcessorService assignmentEventProcessorService)
{
    [Function(nameof(PullRequestOpenedEventFunction))]
    public async Task Run(
        [QueueTrigger("ahkstatustracking-pull-request-opened", Connection = "AHK_EventsQueueConnectionString")]
        PullRequestOpened pullRequestOpened)
    {
        logger.LogInformation(
            $"Pull request opened event function triggered for repo url: {pullRequestOpened.GitHubRepositoryUrl}");
        await assignmentEventProcessorService.ConsumePullRequestOpenedEventAsync(pullRequestOpened);
        logger.LogInformation(
            $"Pull request opened event consumed for repo url: {pullRequestOpened.GitHubRepositoryUrl}");
    }
}
