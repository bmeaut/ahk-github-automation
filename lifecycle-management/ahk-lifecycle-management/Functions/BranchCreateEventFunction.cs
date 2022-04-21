using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.Lifecycle.Management
{
    public static class BranchCreateEventFunction
    {
        [FunctionName("BranchCreateEventFunction")]
        public static void Run([QueueTrigger("ahk-branch-create", Connection = "AHK_EventsQueueConnectionString")]BranchCreateEvent data, ILogger log)
        {
            log.LogInformation($"BranchCreateEventFunction for {data}");
        }
    }
}
