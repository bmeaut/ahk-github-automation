using Ahk.GradeManagement.Shared.Enums;

namespace Ahk.GradeManagement.Shared.Dtos;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public string GithubId { get; set; }
    public string BmeEmail { get; set; }
    public UserType Type { get; set; }
}
