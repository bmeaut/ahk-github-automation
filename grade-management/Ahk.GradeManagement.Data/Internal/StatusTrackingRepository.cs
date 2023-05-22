using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Data.Internal;

namespace Ahk.Grademanagement.Data.Internal
{
    public class StatusTrackingRepository : IStatusTrackingRepository
    {
        public AhkDbContext Context { get; set; }
        public StatusTrackingRepository(AhkDbContext context)
        {
            Context = context;
        }

        public Task InsertNewEvent(StatusEventBase data)
        {
            if (data.Type == "WorkflowRunEvent")
            {
                Context.WorkflowRunEvents.AddAsync((WorkflowRunEvent)data);
            }
            if (data.Type == "PullRequestEvent")
            {
                Context.PullRequestEvents.AddAsync((PullRequestEvent)data);
            }
            if (data.Type == "RepositoryCreateEvent")
            {
                Context.RepositoryCreateEvents.AddAsync((RepositoryCreateEvent)data);
            }
            if (data.Type == "BranchCreateEvent")
            {
                Context.BranchCreateEvents.AddAsync((BranchCreateEvent)data);
            }
            return Context.SaveChangesAsync();
        }
        public async Task<IReadOnlyCollection<StatusEventBase>> ListEventsForRepositories(string prefix)
        {
            var workflowEvents = Context.WorkflowRunEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();
            var pullrequestEvents = Context.PullRequestEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();
            var repositoryCreateEvents = Context.RepositoryCreateEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();
            var branchCreateEvents = Context.BranchCreateEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();

            List<StatusEventBase> events = workflowEvents.Concat(pullrequestEvents).Concat(repositoryCreateEvents).Concat(branchCreateEvents).ToList();

            return events.AsReadOnly();
        }
    }
}
