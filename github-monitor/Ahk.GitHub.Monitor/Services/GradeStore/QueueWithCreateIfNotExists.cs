using System.Threading.Tasks;
using Azure.Storage.Queues;

namespace Ahk.GitHub.Monitor.Services
{
    internal class QueueWithCreateIfNotExists
    {
        private readonly QueueClient queue;
        private volatile bool queueCreated = false;

        public QueueWithCreateIfNotExists(QueueServiceClient queueService, string queueName)
            => this.queue = queueService.GetQueueClient(queueName);

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
