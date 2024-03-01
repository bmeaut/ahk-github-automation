namespace GradeManagement.Shared.DTOs;

public class TeacherDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public string GithubId { get; set; }
    public string BmeEmail { get; set; }
    public List<CourseTeacherDTO> CourseTeacherDtos { get; set; }
}
