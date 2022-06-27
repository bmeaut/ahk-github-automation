using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.StatusTracking
{
    public class StatusTrackingService : IStatusTrackingService
    {
        private readonly IStatusTrackingRepository repo;

        public StatusTrackingService(IStatusTrackingRepository repo) => this.repo = repo;

        public Task InsertNewEvent(StatusEventBase data) => this.repo.InsertNewEvent(data);

        public async Task<IReadOnlyCollection<RepositoryStatus>> ListStatusForRepositories(string repoPrefix)
        {
            var events = await this.repo.ListEventsForRepositories(repoPrefix);
            return events
                .GroupBy(e => e.Repository)
                .Select(createStatus)
                .ToList();
        }

        private static RepositoryStatus createStatus(IGrouping<string, StatusEventBase> events)
        {
            return new RepositoryStatus(
                repository: events.Key,
                branches: getBranches(events),
                pullRequests: getPrStatuses(events),
                workflowRuns: getWorkflowRunsStatus(events));
        }

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
