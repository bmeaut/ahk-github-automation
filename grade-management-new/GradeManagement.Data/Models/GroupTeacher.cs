namespace GradeManagement.Data.Models;

public class GroupTeacher
{
    public long Id { get; set; }
    public User User { get; set; }
    public long UserId { get; set; }
    public Group Group { get; set; }
    public long GroupId { get; set; }
}
