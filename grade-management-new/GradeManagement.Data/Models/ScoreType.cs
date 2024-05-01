namespace GradeManagement.Data.Models;

public class ScoreType : ISoftDelete
{
    public long Id { get; set; }
    public string Type { get; set; }
    public List<Score> Scores { get; set; }
    public bool IsDeleted { get; set; }
}
