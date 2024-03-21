namespace GradeManagement.Data.Models;

public class Group
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<GroupStudent> GroupStudents { get; set; }
    public List<CourseTeacher> CourseTeachers { get; set; }
}
