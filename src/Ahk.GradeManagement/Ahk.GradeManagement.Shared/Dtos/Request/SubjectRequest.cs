using Ahk.GradeManagement.Shared.Dtos;

namespace Ahk.GradeManagement.Shared.Dtos.Request;

public class SubjectRequest
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string GitHubOrgName { get; set; }
    public string NeptunCode { get; set; }
    public List<User>? Teachers { get; set; }
    public string? CiApiKey { get; set; }
}
