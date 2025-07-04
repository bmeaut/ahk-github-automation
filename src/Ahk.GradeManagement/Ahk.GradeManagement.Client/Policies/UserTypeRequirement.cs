using Ahk.GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

namespace Ahk.GradeManagement.Client.Policies;

public class UserTypeRequirement(UserType[] type) : IAuthorizationRequirement
{
    public UserType[] Type { get; } = type;
}
