namespace GradeManagement.Shared.Dtos;

public class Course
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string MoodleCourseId { get; set; }
    public long SubjectId { get; set; }
    public Semester Semester { get; set; }
    public Language Language { get; set; }
}
