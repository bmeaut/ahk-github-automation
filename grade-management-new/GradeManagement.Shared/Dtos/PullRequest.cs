namespace GradeManagement.Shared.Dtos;

public class PullRequest
{
    public long Id { get; set; }
    public string Url { get; set; }
    public DateTimeOffset OpeningDate { get; set; }
    public bool IsClosed { get; set; }
    public string BranchName { get; set; }
    public Assignment Assignment { get; set; }
    public long AssignmentId { get; set; }
    public List<AssignmentEvent> AssignmentEvents { get; set; }
}
