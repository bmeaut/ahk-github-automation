using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class Course
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public string Name { get; set; }
    public string MoodleCourseId { get; set; }
    public Semester Semester { get; set; }
    public Subject Subject { get; set; }
    public Language Language { get; set; }
    public List<CourseTeacher> CourseTeachers { get; set; }
    public List<Group> Groups { get; set; }
    public List<Task> Tasks { get; set; }
}
