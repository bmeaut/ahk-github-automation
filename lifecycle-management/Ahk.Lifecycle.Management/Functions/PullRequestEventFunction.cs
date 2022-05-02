using System.Threading.Tasks;
using Ahk.Lifecycle.Management.DAL;
using Ahk.Lifecycle.Management.DAL.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.Lifecycle.Management
{
    public class PullRequestEventFunction
    {
        private readonly IRepository service;

        public PullRequestEventFunction(IRepository service) => this.service = service;

        [FunctionName("PullRequestEventFunction")]
        public async Task Run([QueueTrigger("ahk-pull-request", Connection = "AHK_EventsQueueConnectionString")]PullRequestEvent data, ILogger log)
        {
            log.LogInformation("PullRequestEventFunction triggered for Repository='{Repository}', Username='{Username}', Action='{Action}', Assignees='{Assignees}', Neptun='{Neptun}'", data.Repository, data.Username, data.Action, data.Assignees, data.Neptun);
            await service.Insert(data);
        }
    }
}
