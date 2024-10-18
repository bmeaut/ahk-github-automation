using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.AzureQueues;
using Ahk.GitHub.Monitor.Services.GradeStore.Dto;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

namespace Ahk.GitHub.Monitor.Services
{
    internal class GradeStoreAzureQueue : IGradeStore
    {
        private const string QueueNameSetGrade = "ahksetgrade";
        private const string QueueNameConfirmAutoGrade = "ahkconfirmautograde";

        private readonly QueueWithCreateIfNotExists queueSetGrade;
        private readonly QueueWithCreateIfNotExists queueConfirmAutoGrade;

        public GradeStoreAzureQueue(IAzureClientFactory<QueueServiceClient> clientFactory)
        {
            var queueService = clientFactory.CreateClient(QueueClientName.Name);
            queueSetGrade = new QueueWithCreateIfNotExists(queueService, QueueNameSetGrade);
            queueConfirmAutoGrade = new QueueWithCreateIfNotExists(queueService, QueueNameConfirmAutoGrade);
        }

        public Task StoreGrade(string repositoryUrl, string prUrl, string actor, Dictionary<int, double> results)
        {
            var e = new AssignmentGradedByTeacher(repositoryUrl, prUrl, actor, results, System.DateTimeOffset.UtcNow);
            return queueSetGrade.Send(e);
        }

        public Task ConfirmAutoGrade(string repositoryUrl, string prUrl, string actor)
        {
            var e = new AssignmentGradedByTeacher(repositoryUrl, prUrl, actor, [], System.DateTimeOffset.UtcNow);
            return queueConfirmAutoGrade.Send(e);
        }
    }
}
