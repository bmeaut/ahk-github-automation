using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services
{
    public interface IEventDispatchService
    {
        Task Process(string githubEventName, string requestBody, WebhookResult webhookResult);
    }
}