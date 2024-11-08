namespace GradeManagement.Shared.Dtos.Response;

public class ExerciseResponse
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string GithubPrefix { get; set; }
    public DateTimeOffset dueDate { get; set; }
    public long CourseId { get; set; }
    public Dictionary<int, string> ScoreTypes { get; set; } // Key: ScoreType order in exercise, Value: ScoreType.Type
}
