namespace GradeManagement.Shared.Dtos;

public class Group
{
    public long Id { get; set; }
    public string Name { get; set; }
    public Course Course { get; set; }
    public long CourseId { get; set; }
    public List<GroupStudent> GroupStudents { get; set; }
    public List<CourseTeacher> CourseTeachers { get; set; }
}
