using Ahk.GradeManagement.Backend.Common.RequestContext;

namespace Ahk.GradeManagement.Api.RequestContext;

public static class RequestContextExtensions
{
    public static IServiceCollection AddRequestContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IRequestContext, HttpRequestContext>();

        return services;
    }
}
