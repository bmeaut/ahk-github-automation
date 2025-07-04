using Ahk.GradeManagement.Api.Authorization.Handlers;
using Ahk.GradeManagement.Api.Authorization.Policies;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Ahk.GradeManagement.Api.Authorization;

public static class AuthorizationCollectionExtensions
{
    public static void AddPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AdminRequirement.PolicyName, policy => policy.Requirements.Add(new AdminRequirement()));
        services.AddAuthorizationBuilder()
            .AddPolicy(DemonstratorOnSubjectRequirement.PolicyName,
                policy => policy.Requirements.Add(new DemonstratorOnSubjectRequirement()));
        services.AddAuthorizationBuilder()
            .AddPolicy(TeacherOnSubjectRequirement.PolicyName,
                policy => policy.Requirements.Add(new TeacherOnSubjectRequirement()));
        services.AddAuthorizationBuilder()
            .AddPolicy(TeacherRequirement.PolicyName, policy => policy.Requirements.Add(new TeacherRequirement()));
        services.AddAuthorizationBuilder()
            .AddPolicy(UserRequirement.PolicyName, policy => policy.Requirements.Add(new UserRequirement()));
    }

    public static void AddRequirementHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, AdminRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, DemonstratorOnSubjectRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, TeacherOnSubjectRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, TeacherRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, UserRequirementHandler>();
    }

    public static void AddClaimsTransformation(this IServiceCollection services) =>
        services.AddTransient<IClaimsTransformation, UserClaimsTransformation>();
}
