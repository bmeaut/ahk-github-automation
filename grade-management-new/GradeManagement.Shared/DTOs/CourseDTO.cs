using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class CourseDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string MoodleCourseId { get; set; }
    public SemesterDTO SemesterDto { get; set; }
    public SubjectDTO SubjectDto { get; set; }
    public LanguageDTO LanguageDto { get; set; }
    public List<CourseTeacherDTO> CourseTeacherDtos { get; set; }
    public List<GroupDTO> GroupDtos { get; set; }
    public List<TaskDTO> TaskDtos { get; set; }
}
