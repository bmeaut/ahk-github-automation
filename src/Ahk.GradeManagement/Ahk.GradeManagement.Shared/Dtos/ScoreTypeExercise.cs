namespace Ahk.GradeManagement.Shared.Dtos;

public class ScoreTypeExercise
{
    public long Id { get; set; }
    public long ExerciseId { get; set; }
    public ScoreType ScoreType { get; set; }
    public int Order { get; set; }
}
