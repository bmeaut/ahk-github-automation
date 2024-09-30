using GradeManagement.Data.Models.Interfaces;

namespace GradeManagement.Data.Models;

public class Exercise : ISoftDelete, ITenant
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string GithubPrefix { get; set; }
    public DateTimeOffset dueDate { get; set; }
    public Course Course { get; set; }
    public long CourseId { get; set; }
    public List<Assignment> Assignments { get; set; }
    public List<ScoreTypeExercise> ScoreTypeExercises { get; set; }
    public bool IsDeleted { get; set; }
    public long SubjectId { get; set; }
}
