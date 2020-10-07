using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public interface IGitHubEventHandler
    {
        Task Execute(string requestBody, WebhookResult webhookResult);
    }
}