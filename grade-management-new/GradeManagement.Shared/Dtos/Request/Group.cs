namespace GradeManagement.Shared.Dtos.Request;

public class Group
{
    public long Id { get; set; }
    public string Name { get; set; }
    public long CourseId { get; set; }
    public List<User> Teachers { get; set; }
}
