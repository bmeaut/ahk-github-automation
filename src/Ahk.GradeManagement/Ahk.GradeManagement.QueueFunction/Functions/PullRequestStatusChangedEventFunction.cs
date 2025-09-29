using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.QueueFunction.Functions;

public class PullRequestStatusChangedEventFunction(ILogger<PullRequestStatusChangedEventFunction> logger,
    AssignmentEventProcessorService assignmentEventProcessorService)
{
    [Function(nameof(PullRequestStatusChangedEventFunction))]
    public async Task Run([QueueTrigger("ahkstatustracking-pull-request-status-changed", Connection = "ahk-queue-storage")] PullRequestStatusChanged pullRequestStatusChanged)
    {
        logger.LogInformation("Pull request status changed event function triggered for repo url: {RepoUrl}", pullRequestStatusChanged.GitHubRepositoryUrl);
        await assignmentEventProcessorService.ConsumePullRequestStatusChangedEventAsync(pullRequestStatusChanged);
        logger.LogInformation("Pull request status changed event consumed for repo url: {RepoUrl}", pullRequestStatusChanged.GitHubRepositoryUrl);
    }
}
