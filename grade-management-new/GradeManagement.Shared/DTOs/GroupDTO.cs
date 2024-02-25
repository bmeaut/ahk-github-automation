using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class GroupDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public CourseDTO CourseDto { get; set; }
    public List<CourseTeacherDTO> CourseTeacherDtos { get; set; }
    public List<GroupStudentDTO> GroupStudentDtos { get; set; }
}
