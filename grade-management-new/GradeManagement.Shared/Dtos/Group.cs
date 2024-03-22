namespace GradeManagement.Shared.Dtos;

public class Group
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<GroupStudent> GroupStudents { get; set; }
    public List<GroupTeacher> GroupTeachers { get; set; }
}
