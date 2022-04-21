using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.Lifecycle.Management
{
    public static class RepositoryCreateEventFunction
    {
        [FunctionName("RepositoryCreateEventFunction")]
        public static void Run([QueueTrigger("ahk-repository-create", Connection = "AHK_EventsQueueConnectionString")]RepositoryCreateEvent data, ILogger log)
        {
            log.LogInformation($"RepositoryCreateEventFunction for {data}");
        }
    }
}
