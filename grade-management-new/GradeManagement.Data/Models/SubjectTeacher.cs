namespace GradeManagement.Data.Models;

public class SubjectTeacher
{
    public long Id { get; set; }
    public Subject Subject { get; set; }
    public long SubjectId { get; set; }
    public Teacher Teacher { get; set; }
    public long TeacherId { get; set; }
}
