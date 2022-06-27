using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.AzureQueues;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

namespace Ahk.GitHub.Monitor.Services
{
    internal class GradeStoreAzureQueue : IGradeStore
    {
        public const string QueueNameSetGrade = "ahksetgrade";
        public const string QueueNameConfirmAutoGrade = "ahkconfirmautograde";

        private readonly QueueWithCreateIfNotExists queueSetGrade;
        private readonly QueueWithCreateIfNotExists queueConfirmAutoGrade;

        public GradeStoreAzureQueue(IAzureClientFactory<QueueServiceClient> clientFactory)
        {
            var queueService = clientFactory.CreateClient(QueueClientName.Name);
            this.queueSetGrade = new QueueWithCreateIfNotExists(queueService, QueueNameSetGrade);
            this.queueConfirmAutoGrade = new QueueWithCreateIfNotExists(queueService, QueueNameConfirmAutoGrade);
        }

        public Task StoreGrade(string neptun, string repository, int prNumber, string prUrl, string actor, string origin, IReadOnlyCollection<double> results)
        {
            var e = new SetGradeEvent(neptun, repository, prNumber, prUrl, actor, origin, results.ToArray());
            return queueSetGrade.Send(e);
        }

        public Task ConfirmAutoGrade(string neptun, string repository, int prNumber, string prUrl, string actor, string origin)
        {
            var e = new ConfirmAutoGradeEvent(neptun, repository, prNumber, prUrl, actor, origin);
            return queueConfirmAutoGrade.Send(e);
        }
    }
}
