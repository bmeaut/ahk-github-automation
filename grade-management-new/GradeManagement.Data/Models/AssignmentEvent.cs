using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GradeManagement.Shared;

namespace GradeManagement.Data.Models;

public class AssignmentEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public EventType EventType { get; set; }
    public string Description { get; set; }
    public long AssignmentId { get; set; }
    public Assignment Assignment { get; set; }
    public PullRequest PullRequest { get; set; }
}
