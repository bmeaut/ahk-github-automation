using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GradeManagement.Shared.DTOs;

namespace GradeManagement.Data.Models;

public class ScoreDTO
{
    public long Id { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public AssignmentDTO AssignmentDto { get; set; }
}
