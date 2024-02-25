namespace GradeManagement.Shared.DTOs;

public class AssignmentEventDTO
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public EventType EventType { get; set; }
    public string Description { get; set; }
    //public AssignmentDTO AssignmentDto { get; set; }
    public PullRequestDTO PullRequestDto { get; set; }
}
