using Ahk.GradeManagement.Dal.Entities.Interfaces;

namespace Ahk.GradeManagement.Dal.Entities;

public class Exercise : ISoftDelete, ITenant
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string GithubPrefix { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public string? MoodleScoreUrl { get; set; }
    public string MoodleScoreNamePrefix { get; set; }
    public string ClassroomUrl { get; set; }
    public Course Course { get; set; }
    public long CourseId { get; set; }
    public List<Assignment> Assignments { get; set; }
    public List<ScoreTypeExercise> ScoreTypeExercises { get; set; }
    public bool IsDeleted { get; set; }
    public long SubjectId { get; set; }
}
