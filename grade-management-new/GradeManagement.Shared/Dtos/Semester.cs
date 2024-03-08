namespace GradeManagement.Shared.Dtos;

public class Semester
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<Course> Courses { get; set; }
}
