using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GradeManagement.Shared.DTOs;

namespace GradeManagement.Data.Models;

public class TaskDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string githubPrefix { get; set; }
    public CourseDTO CourseDto { get; set; }
    public List<AssignmentDTO> AssignmentDtos { get; set; }
}
