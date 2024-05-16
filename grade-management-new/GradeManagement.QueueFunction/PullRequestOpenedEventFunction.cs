using Azure.Storage.Queues.Models;

using GradeManagement.Bll;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GradeManagement.QueueFunction;

public class PullRequestOpenedEventFunction
{
    private readonly ILogger<AssignmentAcceptedEventFunction> _logger;
    private readonly AssignmentEventProcessorService _assignmentEventProcessorService;

    public PullRequestOpenedEventFunction(ILogger<AssignmentAcceptedEventFunction> logger,
        AssignmentEventProcessorService assignmentEventProcessorService)
    {
        _logger = logger;
        _assignmentEventProcessorService = assignmentEventProcessorService;
    }

    [Function(nameof(PullRequestOpenedEventFunction))]
    public async Task Run(
        [QueueTrigger("pull-request-opened", Connection = "AHK_EventsQueueConnectionString")]
        PullRequestOpened pullRequestOpened)
    {
        _logger.LogInformation(
            $"Pull request opened event function triggered for repo url: {pullRequestOpened.GitHubRepositoryUrl}");
        await _assignmentEventProcessorService.ConsumePullRequestOpenedEventAsync(pullRequestOpened);
        _logger.LogInformation(
            $"Pull request opened event consumed for repo url: {pullRequestOpened.GitHubRepositoryUrl}");
    }
}
