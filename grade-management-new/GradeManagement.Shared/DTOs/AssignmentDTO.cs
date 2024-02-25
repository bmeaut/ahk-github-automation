namespace GradeManagement.Shared.DTOs;

public class AssignmentDTO
{
    public long Id { get; set; }
    public string GithubRepoName { get; set; }
    public StudentDTO StudentDto { get; set; }
    public TaskDTO TaskDto { get; set; }
    public List<PullRequestDTO> PullRequestDtos { get; set; }
    public List<ScoreDTO> ScoreDtos { get; set; }
    public List<AssignmentEventDTO> AssignmentEventDtos { get; set; }
}
