using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class GroupStudentDTO
{
    public long Id { get; set; }
    public GroupDTO GroupDto { get; set; }
    public StudentDTO StudentDto { get; set; }
}
