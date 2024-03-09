namespace GradeManagement.Data.Models;

public class GroupStudent
{
    public long Id { get; set; }
    public Group Group { get; set; }
    public long GroupId { get; set; }
    public Student Student { get; set; }
    public long StudentId { get; set; }
}
