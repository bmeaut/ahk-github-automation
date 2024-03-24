namespace GradeManagement.Data.Models;

public class Group : ISoftDelete
{
    public long Id { get; set; }
    public string Name { get; set; }
    public long CourseId { get; set; }
    public Course Course { get; set; }
    public List<GroupStudent> GroupStudents { get; set; }
    public List<GroupTeacher> GroupTeachers { get; set; }
    public bool IsDeleted { get; set; }
}
