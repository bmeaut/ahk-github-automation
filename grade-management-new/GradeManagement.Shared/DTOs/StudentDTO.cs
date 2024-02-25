using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GradeManagement.Shared.DTOs;

namespace GradeManagement.Data.Models;

public class StudentDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public List<GroupStudentDTO> GroupStudentDtos { get; set; }
    public List<AssignmentDTO> AssignmentDtos { get; set; }
}
