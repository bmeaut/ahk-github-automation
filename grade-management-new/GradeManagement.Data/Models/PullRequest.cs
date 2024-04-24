namespace GradeManagement.Data.Models;

public class PullRequest : ISoftDelete
{
    public long Id { get; set; }
    public string Url { get; set; }
    public DateTimeOffset OpeningDate { get; set; }
    public bool IsClosed { get; set; }
    public string BranchName { get; set; }
    public Assignment Assignment { get; set; }
    public long AssignmentId { get; set; }
    public List<AssignmentLog> AssignmentEvents { get; set; }
    public bool IsDeleted { get; set; }
}
