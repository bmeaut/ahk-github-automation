namespace GradeManagement.Shared.Dtos;

public class CourseTeacher
{
    public long Id { get; set; }
    public Course Course { get; set; }
    public long CourseId { get; set; }
    public Teacher Teacher { get; set; }
    public long TeacherId { get; set; }
    public Group Group { get; set; }
    public long GroupId { get; set; }
}
