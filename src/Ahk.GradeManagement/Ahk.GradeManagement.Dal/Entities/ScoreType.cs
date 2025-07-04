using Ahk.GradeManagement.Dal.Entities.Interfaces;

namespace Ahk.GradeManagement.Dal.Entities;

public class ScoreType : ISoftDelete
{
    public long Id { get; set; }
    public string Type { get; set; }
    public List<Score> Scores { get; set; }
    public List<ScoreTypeExercise> ScoreTypeExercises { get; set; }
    public bool IsDeleted { get; set; }
}
