namespace GradeManagement.Shared.Dtos;

public class Score
{
    public long Id { get; set; }
    public string Value { get; set; }
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public ScoreType ScoreType { get; set; }
    public long AssignmentId { get; set; }
}
