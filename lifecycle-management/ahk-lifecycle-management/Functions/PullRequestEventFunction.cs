using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.Lifecycle.Management
{
    public static class PullRequestEventFunction
    {
        [FunctionName("PullRequestEventFunction")]
        public static void Run([QueueTrigger("ahk-pull-request", Connection = "AHK_EventsQueueConnectionString")]PullRequestEvent data, ILogger log)
        {
            log.LogInformation($"PullRequestEventFunction for {data}");
        }
    }
}
