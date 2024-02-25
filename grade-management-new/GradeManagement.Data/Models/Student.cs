using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class Student
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public List<GroupStudent> GroupStudents { get; set; }
    public List<Assignment> Assignments { get; set; }
}
