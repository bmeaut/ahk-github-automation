using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Data.Models;
using Ahk.GradeManagement.StatusTracking;

namespace Ahk.GradeManagement.Services.StatusTrackingService
{
    public class StatusTrackingService : IStatusTrackingService
    {
        public AhkDbContext Context { get; set; }

        public StatusTrackingService(AhkDbContext context) => this.Context = context;

        public Task InsertNewEventAsync(StatusEventBase data)
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

        public async Task<IReadOnlyCollection<SubmissionInfo>> ListStatusForRepositoriesAsync(string prefix)
        {
            var workflowEvents = Context.WorkflowRunEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();
            var pullrequestEvents = Context.PullRequestEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();
            var repositoryCreateEvents = Context.RepositoryCreateEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();
            var branchCreateEvents = Context.BranchCreateEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();

            var events = workflowEvents.Concat(pullrequestEvents).Concat(repositoryCreateEvents).Concat(branchCreateEvents).ToList();

            return events
                .GroupBy(e => e.Repository)
                .Select(createStatus)
                .ToList();
        }

        public async Task<IReadOnlyCollection<StatusEventBase>> ListEventsForRepositoryAsync(string prefix)
        {
            var workflowEvents = Context.WorkflowRunEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();
            var pullrequestEvents = Context.PullRequestEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();
            var repositoryCreateEvents = Context.RepositoryCreateEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();
            var branchCreateEvents = Context.BranchCreateEvents.Where(e => e.Repository.StartsWith(prefix)).ToList().Cast<StatusEventBase>().ToList();

            var events = workflowEvents.Concat(pullrequestEvents).Concat(repositoryCreateEvents).Concat(branchCreateEvents).ToList();

            return events.AsReadOnly();
        }

        private static SubmissionInfo createStatus(IGrouping<string, StatusEventBase> events)
        {
            return new SubmissionInfo(
                repository: events.Key,
                neptun: getNeptun(events),
                branches: getBranches(events),
                pullRequests: getPrStatuses(events),
                workflowRuns: getWorkflowRunsStatus(events));
        }

        private static string getNeptun(IEnumerable<StatusEventBase> events)
            => events.OfType<PullRequestEvent>()
                     .Where(e => !string.IsNullOrEmpty(e.Neptun))
                     .OrderByDescending(e => e.Timestamp)
                     .Select(e => e.Neptun)
                     .FirstOrDefault() ?? string.Empty;

        private static IReadOnlyCollection<string> getBranches(IEnumerable<StatusEventBase> events)
            => events.OfType<BranchCreateEvent>().Select(e => e.Branch).Distinct().ToArray();

        private static IReadOnlyCollection<PullRequestStatus> getPrStatuses(IEnumerable<StatusEventBase> events)
            => events.OfType<PullRequestEvent>().GroupBy(e => e.Number).Select(getPrStatus).ToArray();

        private static PullRequestStatus getPrStatus(IGrouping<int, PullRequestEvent> events)
        {
            return new PullRequestStatus(
                number: events.Key,
                htmlUrl: events.OrderByDescending(e => e.Timestamp).First().HtmlUrl,
                status: events.OrderByDescending(e => e.Timestamp).First().Action,
                assignee: string.Join(", ", events.Where(e => e.Assignees != null).SelectMany(e => e.Assignees).Distinct()));
        }

        private static WorkflowRunsStatus getWorkflowRunsStatus(IEnumerable<StatusEventBase> events)
        {
            var items = events.OfType<WorkflowRunEvent>();
            return new WorkflowRunsStatus(
                count: items.Count(),
                lastStatus: items.OrderByDescending(e => e.Timestamp).FirstOrDefault()?.Conclusion);
        }
    }
}
