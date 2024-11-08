using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Event is a GitHub terminology in this context.")]
public interface IGitHubEventHandler
{
    Task<EventHandlerResult> Execute(string requestBody);
}
