using Ahk.GradeManagement.Shared.Enums;

namespace Ahk.GradeManagement.Shared.Dtos;

public class PullRequestForDashboard
{
    public long Id { get; set; }
    public string Url { get; set; }
    public DateTimeOffset OpeningDate { get; set; }
    public PullRequestStatus Status { get; set; } = PullRequestStatus.Open;
    public string BranchName { get; set; }
    public long AssignmentId { get; set; }
    public long? TeacherId { get; set; }
    public List<Score> Scores { get; set; }
}
