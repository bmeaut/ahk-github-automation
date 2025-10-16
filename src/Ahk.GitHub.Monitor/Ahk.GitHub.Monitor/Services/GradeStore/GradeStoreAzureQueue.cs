using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.AzureQueues;
using Ahk.GitHub.Monitor.Services.GradeStore.Dto;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

namespace Ahk.GitHub.Monitor.Services.GradeStore;

internal class GradeStoreAzureQueue : IGradeStore
{
    private const string QueueName = "ahkstatustracking-assignment-graded-by-teacher";

    private readonly QueueWithCreateIfNotExists _queue;

    public GradeStoreAzureQueue(IAzureClientFactory<QueueServiceClient> clientFactory)
    {
        var queueService = clientFactory.CreateClient(QueueClientName.Name);
        _queue = new QueueWithCreateIfNotExists(queueService, QueueName);
    }

    public Task StoreGradeAsync(string repositoryUrl, string prUrl, long pullRequestGitHubId, string actor, Dictionary<int, double> results)
    {
        return _queue.Send(new AssignmentGradedByTeacher
        {
            PullRequestGitHubId = pullRequestGitHubId,
            DateOfGrading = System.DateTimeOffset.UtcNow,
            PullRequestUrl = prUrl,
            GitHubRepositoryUrl = repositoryUrl,
            TeacherGitHubId = actor,
            Scores = results,
        });
    }

    public Task ConfirmAutoGradeAsync(string repositoryUrl, string prUrl, long pullRequestGitHubId, string actor)
    {
        return _queue.Send(new AssignmentGradedByTeacher
        {
            PullRequestGitHubId = pullRequestGitHubId,
            GitHubRepositoryUrl = repositoryUrl,
            PullRequestUrl = prUrl,
            TeacherGitHubId = actor,
            Scores = [],
            DateOfGrading = System.DateTimeOffset.UtcNow
        });
    }
}
