using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GradeManagement.Shared;
using GradeManagement.Shared.DTOs;

namespace GradeManagement.Data.Models;

public class AssignmentEventDTO
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public EventType EventType { get; set; }
    public string Description { get; set; }
    public AssignmentDTO AssignmentDto { get; set; }
    public PullRequestDTO PullRequestDto { get; set; }
}
