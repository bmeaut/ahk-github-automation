using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ahk.GitHub.Monitor.Services
{
    public interface IEventDispatchService
    {
        Task Process(string githubEventName, string requestBody, WebhookResult webhookResult, ILogger logger);
    }
}