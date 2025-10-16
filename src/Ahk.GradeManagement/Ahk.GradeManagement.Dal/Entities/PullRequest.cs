using Ahk.GradeManagement.Dal.Entities.Interfaces;
using Ahk.GradeManagement.Shared.Enums;

namespace Ahk.GradeManagement.Dal.Entities;

public class PullRequest : ISoftDelete, ITenant
{
    public long Id { get; set; }
    public long GitHubId { get; set; }
    public string Url { get; set; }
    public DateTimeOffset OpeningDate { get; set; }
    public PullRequestStatus Status { get; set; } = PullRequestStatus.Open;
    public string BranchName { get; set; }
    public Assignment Assignment { get; set; }
    public long AssignmentId { get; set; }
    public User? Teacher { get; set; }
    public long? TeacherId { get; set; }
    public List<AssignmentLog> AssignmentLogs { get; set; }
    public List<Score> Scores { get; set; }
    public bool IsDeleted { get; set; }
    public long SubjectId { get; set; }
}
