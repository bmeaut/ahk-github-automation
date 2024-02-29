using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class Group
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public string Name { get; set; }
    public Course Course { get; set; }
    public List<GroupStudent> GroupStudents { get; set; }
    public List<CourseTeacher> CourseTeachers { get; set; }
}
