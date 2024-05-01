using GradeManagement.Shared.Enums;

namespace GradeManagement.Shared.Dtos.AssignmentEvents;

public class PullRequestStatusChanged
{
    public string GitHubRepositoryUrl { get; set; }
    public string PullRequestUrl { get; set; }
    public PullRequestStatus pullRequestStatus {get; set;}
}
