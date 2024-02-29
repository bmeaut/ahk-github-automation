using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class Assignment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public string GithubRepoName { get; set; }
    public Student Student { get; set; }
    public Task Task { get; set; }
    public List<PullRequest> PullRequests { get; set; }
    public List<Score> Scores { get; set; }
    public List<AssignmentEvent> AssignmentEvents { get; set; }
}
