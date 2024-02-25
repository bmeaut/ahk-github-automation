namespace GradeManagement.Shared.DTOs;

public class SemesterDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<CourseDTO> CourseDtos { get; set; }
}
