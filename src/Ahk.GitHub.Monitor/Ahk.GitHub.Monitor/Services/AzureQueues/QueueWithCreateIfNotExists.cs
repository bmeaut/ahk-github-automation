using System.Threading.Tasks;
using Azure.Storage.Queues;

namespace Ahk.GitHub.Monitor.Services.AzureQueues;

internal class QueueWithCreateIfNotExists(QueueServiceClient queueService, string queueName)
{
    private readonly QueueClient _queue = queueService.GetQueueClient(queueName);
    private volatile bool _queueCreated;

    public async Task Send<T>(T value)
    {
        if (!_queueCreated)
        {
            await _queue.CreateIfNotExistsAsync();
            _queueCreated = true;
        }

        await _queue.SendMessageAsync(new System.BinaryData(value));
    }
}
