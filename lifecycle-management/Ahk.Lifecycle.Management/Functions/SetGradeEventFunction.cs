using System.Threading.Tasks;
using Ahk.Lifecycle.Management.DAL;
using Ahk.Lifecycle.Management.DAL.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ahk.Lifecycle.Management
{
    public class SetGradeEventFunction
    {
        private readonly IRepository service;

        public SetGradeEventFunction(IRepository service) => this.service = service;

        [FunctionName("SetGradeEventFunction")]
        public async Task Run([QueueTrigger("ahk-set-grade", Connection = "AHK_EventsQueueConnectionString")]SetGradeEvent data, ILogger log)
        {
            log.LogInformation("SetGradeEventFunction triggered for Repository='{Repository}', Username='{Username}'", data.Repository, data.Username);
            await service.Insert(data);
        }
    }
}
