namespace GradeManagement.Shared.Dtos;

public class ScoreType
{
    public long Id { get; set; }
    public string Type { get; set; }
    public List<Score> Scores { get; set; }
}
