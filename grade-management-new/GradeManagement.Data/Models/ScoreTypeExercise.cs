namespace GradeManagement.Data.Models;

public class ScoreTypeExercise : ISoftDelete
{
    public long Id { get; set; }
    public long ExerciseId { get; set; }
    public Exercise Exercise { get; set; }
    public long ScoreTypeId { get; set; }
    public ScoreType ScoreType { get; set; }
    public int Order { get; set; }
    public bool IsDeleted { get; set; }
}
