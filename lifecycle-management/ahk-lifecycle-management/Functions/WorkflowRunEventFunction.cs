using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.Lifecycle.Management
{
    public static class WorkflowRunEventFunction
    {
        [FunctionName("WorkflowRunEventFunction")]
        public static void Run([QueueTrigger("ahk-workflow-run", Connection = "AHK_EventsQueueConnectionString")]WorkflowRunEvent data, ILogger log)
        {
            log.LogInformation($"WorkflowRunEventFunction for {data}");
        }
    }
}
