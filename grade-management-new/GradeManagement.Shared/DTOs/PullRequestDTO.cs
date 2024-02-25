using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GradeManagement.Shared.DTOs;

namespace GradeManagement.Data.Models;

public class PullRequestDTO
{
    public long Id { get; set; }
    public string Url { get; set; }
    public DateTime OpeningDate { get; set; }
    public bool Closed { get; set; }
    public AssignmentDTO AssignmentDto { get; set; }
    public List<AssignmentEventDTO> AssignmentEventDtos { get; set; }
}
