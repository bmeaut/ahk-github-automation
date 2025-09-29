using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.QueueFunction.Functions;

public class PullRequestOpenedEventFunction(
    ILogger<PullRequestOpenedEventFunction> logger,
    AssignmentEventProcessorService assignmentEventProcessorService)
{
    [Function(nameof(PullRequestOpenedEventFunction))]
    public async Task Run([QueueTrigger("ahkstatustracking-pull-request-opened", Connection = "ahk-queue-storage")] PullRequestOpened pullRequestOpened)
    {
        logger.LogInformation("Pull request opened event function triggered for repo url: {RepoUrl}", pullRequestOpened.GitHubRepositoryUrl);
        await assignmentEventProcessorService.ConsumePullRequestOpenedEventAsync(pullRequestOpened);
        logger.LogInformation("Pull request opened event consumed for repo url: {RepoUrl}", pullRequestOpened.GitHubRepositoryUrl);
    }
}
