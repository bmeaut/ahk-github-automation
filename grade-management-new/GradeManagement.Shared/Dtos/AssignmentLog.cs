using GradeManagement.Shared.Enums;

namespace GradeManagement.Shared.Dtos;

public class AssignmentLog
{
    public long Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public EventType EventType { get; set; }
    public string Description { get; set; }
    public Assignment Assignment { get; set; }
    public long AssignmentId { get; set; }
    public PullRequest PullRequest { get; set; }
    public long PullRequestId { get; set; }
}
