namespace GradeManagement.Shared.Dtos.Request;

public class Subject
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public List<Teacher> Teachers { get; set; }
}
