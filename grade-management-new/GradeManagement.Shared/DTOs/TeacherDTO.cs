using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class TeacherDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public string GithubId { get; set; }
    public string BmeEmail { get; set; }
    public List<CourseTeacherDTO> CourseTeacherDtos { get; set; }
}
