namespace Ahk.GradeManagement.Shared.Dtos.Request;

public class ExerciseRequest
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string GithubPrefix { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public string MoodleScoreNamePrefix { get; set; }
    public string ClassroomUrl { get; set; }
    public long CourseId { get; set; }
    public Dictionary<int, string> ScoreTypes { get; set; } // Key: ScoreType order in exercise, Value: ScoreType.Type
}
