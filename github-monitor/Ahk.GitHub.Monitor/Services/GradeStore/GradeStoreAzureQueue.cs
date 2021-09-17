using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services
{
    internal class GradeStoreAzureQueue : IGradeStore
    {
        public const string QueueClientName = "ahkevents";
        public const string QueueName = "ahksetgrade";

        private readonly QueueClient queue;

        private volatile bool queueCreated = false;

        public GradeStoreAzureQueue(IAzureClientFactory<QueueServiceClient> clientFactory)
        {
            this.queue = clientFactory.CreateClient(QueueClientName).GetQueueClient(QueueName);
        }

        public async Task StoreGrade(string neptun, string repository, int prNumber, string prUrl, string actor, string origin, IReadOnlyCollection<double> results)
        {
            if (!queueCreated)
            {
                await queue.CreateIfNotExistsAsync();
                queueCreated = true;
            }

            var e = new SetGradeEvent(neptun, repository, prNumber, prUrl, actor, origin, results.ToArray());
            await queue.SendMessageAsync(new System.BinaryData(e));
        }
    }
}
