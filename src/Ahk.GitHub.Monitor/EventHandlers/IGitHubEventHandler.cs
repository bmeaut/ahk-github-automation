using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public interface IGitHubEventHandler
    {
        Task<EventHandlerResult> Execute(string requestBody);
    }
}