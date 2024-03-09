namespace GradeManagement.Shared.Dtos;

public class Teacher
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public string GithubId { get; set; }
    public string BmeEmail { get; set; }
    public List<CourseTeacher> CourseTeachers { get; set; }
}
