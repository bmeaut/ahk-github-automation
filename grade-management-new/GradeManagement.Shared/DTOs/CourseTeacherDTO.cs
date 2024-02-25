namespace GradeManagement.Shared.DTOs;

public class CourseTeacherDTO
{
    public long Id { get; set; }
    public CourseDTO CourseDto { get; set; }
    public TeacherDTO TeacherDto { get; set; }
}
