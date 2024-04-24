namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class EventScore
{
    public string Value { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public string ScoreType { get; set; }
}
