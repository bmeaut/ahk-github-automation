using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class CourseTeacherDTO
{
    public long Id { get; set; }
    public CourseDTO CourseDto { get; set; }
    public TeacherDTO TeacherDto { get; set; }
    public GroupDTO GroupDto { get; set; }
}
