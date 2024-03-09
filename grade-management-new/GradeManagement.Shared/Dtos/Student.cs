namespace GradeManagement.Shared.Dtos;

public class Student
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public List<GroupStudent> GroupStudents { get; set; }
    public List<Assignment> Assignments { get; set; }
}
