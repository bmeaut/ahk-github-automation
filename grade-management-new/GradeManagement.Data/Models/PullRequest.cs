using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class PullRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public string Url { get; set; }
    public DateTime OpeningDate { get; set; }
    [DefaultValue(false)]
    public bool Closed { get; set; }
    public Assignment Assignment { get; set; }
    public List<AssignmentEvents> AssignmentEvents { get; set; }
}
