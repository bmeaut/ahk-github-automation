using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GradeManagement.QueueFunction.Functions;

public class PullRequestStatusChangedEventFunction
{
    private readonly ILogger<PullRequestStatusChangedEventFunction> _logger;
    private readonly AssignmentEventProcessorService _assignmentEventProcessorService;

    public PullRequestStatusChangedEventFunction(ILogger<PullRequestStatusChangedEventFunction> logger,
        AssignmentEventProcessorService assignmentEventProcessorService)
    {
        _logger = logger;
        _assignmentEventProcessorService = assignmentEventProcessorService;
    }

    [Function(nameof(PullRequestStatusChangedEventFunction))]
    public async Task Run(
        [QueueTrigger("ahkstatustracking-pull-request-status-changed", Connection = "AHK_EventsQueueConnectionString")]
        PullRequestStatusChanged pullRequestStatusChanged)
    {
        _logger.LogInformation(
            $"Pull request status changed event function triggered for repo url: {pullRequestStatusChanged.GitHubRepositoryUrl}");
        await _assignmentEventProcessorService.ConsumePullRequestStatusChangedEventAsync(pullRequestStatusChanged);
        _logger.LogInformation(
            $"Pull request status changed event consumed for repo url: {pullRequestStatusChanged.GitHubRepositoryUrl}");
    }
}
