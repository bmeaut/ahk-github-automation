namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class EventScore
{
    public long Value { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public string ScoreType { get; set; }
}
