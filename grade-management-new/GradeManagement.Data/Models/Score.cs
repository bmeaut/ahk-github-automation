namespace GradeManagement.Data.Models;

public class Score : ISoftDelete
{
    public long Id { get; set; }
    public string Value { get; set; }
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public ScoreType ScoreType { get; set; }
    public long ScoreTypeId { get; set; }
    public Assignment Assignment { get; set; }
    public long AssignmentId { get; set; }
    public bool IsDeleted { get; set; }
}
