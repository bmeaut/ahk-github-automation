namespace GradeManagement.Data.Models;

public class Score : ISoftDelete
{
    public long Id { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public Assignment Assignment { get; set; }
    public long AssignmentId { get; set; }
    public bool IsDeleted { get; set; }
}
