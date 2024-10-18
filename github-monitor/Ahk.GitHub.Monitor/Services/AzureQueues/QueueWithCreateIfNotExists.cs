using System.Threading.Tasks;
using Azure.Storage.Queues;

namespace Ahk.GitHub.Monitor.Services.AzureQueues
{
    internal class QueueWithCreateIfNotExists(QueueServiceClient queueService, string queueName)
    {
        private readonly QueueClient queue = queueService.GetQueueClient(queueName);
        private volatile bool queueCreated;

        public async Task Send<T>(T value)
        {
            if (!queueCreated)
            {
                await queue.CreateIfNotExistsAsync();
                queueCreated = true;
            }

            await queue.SendMessageAsync(new System.BinaryData(value));
        }
    }
}
