namespace Ahk.GradeManagement.Shared.Dtos;

public class Score
{
    public long Id { get; set; }
    public long Value { get; set; }
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public ScoreType ScoreType { get; set; }
    public long? TeacherId { get; set; }
    public long PullRequestId { get; set; }
}
