using System.Threading.Tasks;
using Ahk.Lifecycle.Management.DAL;
using Ahk.Lifecycle.Management.DAL.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.Lifecycle.Management
{
    public class WorkflowRunEventFunction
    {
        private readonly IRepository service;

        public WorkflowRunEventFunction(IRepository service) => this.service = service;

        [FunctionName("WorkflowRunEventFunction")]
        public async Task Run([QueueTrigger("ahk-workflow-run", Connection = "AHK_EventsQueueConnectionString")]WorkflowRunEvent data, ILogger log)
        {
            log.LogInformation("WorkflowRunEventFunction triggered for Repository='{Repository}', Username='{Username}', Conclusion='{Conclusion}'", data.Repository, data.Username, data.Conclusion);
            await service.Insert(data);
        }
    }
}
