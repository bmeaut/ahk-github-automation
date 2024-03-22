namespace GradeManagement.Data.Models;

public class GroupTeacher
{
    public long Id { get; set; }
    public Teacher Teacher { get; set; }
    public long TeacherId { get; set; }
    public Group Group { get; set; }
    public long GroupId { get; set; }
}
