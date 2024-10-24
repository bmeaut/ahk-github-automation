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

    private readonly QueueWithCreateIfNotExists queue;

    public GradeStoreAzureQueue(IAzureClientFactory<QueueServiceClient> clientFactory)
    {
        QueueServiceClient queueService = clientFactory.CreateClient(QueueClientName.Name);
        queue = new QueueWithCreateIfNotExists(queueService, QueueName);
    }

    public Task StoreGrade(string repositoryUrl, string prUrl, string actor, Dictionary<int, double> results)
    {
        var e = new AssignmentGradedByTeacher(repositoryUrl, prUrl, actor, results, System.DateTimeOffset.UtcNow);
        return queue.Send(e);
    }

    public Task ConfirmAutoGrade(string repositoryUrl, string prUrl, string actor)
    {
        var e = new AssignmentGradedByTeacher(repositoryUrl, prUrl, actor, [], System.DateTimeOffset.UtcNow);
        return queue.Send(e);
    }
}
