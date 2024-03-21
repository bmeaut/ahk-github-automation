namespace GradeManagement.Shared.Dtos;

public class Course
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string MoodleCourseId { get; set; }
    public Semester Semester { get; set; }
    public long SemesterId { get; set; }
    public Subject Subject { get; set; }
    public long SubjectId { get; set; }
    public Language Language { get; set; }
    public long LanguageId { get; set; }
    public List<CourseTeacher> CourseTeachers { get; set; }
    public List<Exercise> Tasks { get; set; }
}
