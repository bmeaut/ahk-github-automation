using Ahk.GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

namespace Ahk.GradeManagement.Client.Auth;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddGradeManagementAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMsalAuthentication(options =>
        {
            configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
            foreach (var config in configuration.GetSection("AzureAd:DefaultAccessTokenScopes").AsEnumerable())
            {
                if (!string.IsNullOrWhiteSpace(config.Value) && !options.ProviderOptions.DefaultAccessTokenScopes.Contains(config.Value))
                {
                    options.ProviderOptions.DefaultAccessTokenScopes.Add(config.Value);
                }
            }
        });
        services.AddScoped<IAuthorizationHandler, UserTypeAuthorizationHandler>();
        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(Policy.RequireAdmin, policy => policy.Requirements.Add(new UserTypeRequirement([UserType.Admin])));
            options.AddPolicy(Policy.RequireTeacher, policy => policy.Requirements.Add(new UserTypeRequirement([UserType.Teacher, UserType.Admin])));
        });
        return services;
    }
}
