namespace GradeManagement.Shared.DTOs;

public class ScoreDTO
{
    public long Id { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public AssignmentDTO AssignmentDto { get; set; }
}
