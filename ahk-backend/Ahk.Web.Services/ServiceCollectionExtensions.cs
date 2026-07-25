using System.Net.Http.Headers;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.Courses;
using Ahk.Web.Services.GitHub;
using Ahk.Web.Services.Grading;
using Ahk.Web.Services.Health;
using Ahk.Web.Services.StatusTracking;
using Ahk.Web.Services.Submissions;
using Microsoft.Extensions.DependencyInjection;

namespace Ahk.Web.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the domain services. All are scoped: they use the request's ApplicationDbContext.</summary>
    public static IServiceCollection AddAhkServices(this IServiceCollection services)
    {
        services.AddScoped<ISubmissionResolver, SubmissionResolver>();
        services.AddScoped<ISubmissionEventService, SubmissionEventService>();
        services.AddScoped<IStatusTrackingService, StatusTrackingService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<IGradeListingService, GradeListingService>();
        services.AddScoped<ICourseResolutionService, CourseResolutionService>();
        services.AddScoped<IWebhookTokenService, WebhookTokenService>();

        services.AddAhkGitHubApi();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IAssignmentInviteService, AssignmentInviteService>();
        services.AddScoped<IStudentAssignmentService, StudentAssignmentService>();

        services.AddAhkCourseHealthChecks();

        return services;
    }

    /// <summary>
    /// The GitHub REST transport shared by the assignment flow: a named client based at api.github.com and the
    /// per-course installation-token provider that authenticates it.
    /// </summary>
    public static IServiceCollection AddAhkGitHubApi(this IServiceCollection services)
    {
        services.AddHttpClient(GitHubApiDefaults.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ahk-portal", "1.0"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            // Longer than the health client's 10s: creating a repository from a template is a write, and a
            // student is watching a spinner rather than a dashboard of many courses.
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<ICourseGitHubAppTokenProvider, CourseGitHubAppTokenProvider>();
        services.AddScoped<IGitHubRepositoryService, GitHubRepositoryService>();

        return services;
    }

    /// <summary>
    /// Registers the course health checks and the service that runs them. Adding a check to the admin
    /// dashboard is one line here plus the class itself — the service discovers checks through DI.
    /// </summary>
    public static IServiceCollection AddAhkCourseHealthChecks(this IServiceCollection services)
    {
        services.AddHttpClient(GitHubAccessHealthCheck.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ahk-portal", "1.0"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            // The admin dashboard checks every course in one request; a hung call must not hold it open.
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<ICourseHealthCheck, WebhookConfigurationHealthCheck>();
        services.AddScoped<ICourseHealthCheck, GitHubAccessHealthCheck>();
        services.AddScoped<ICourseHealthCheck, GitHubAppInstallationHealthCheck>();
        services.AddScoped<ICourseHealthCheck, CiCallbackTokenHealthCheck>();
        services.AddScoped<ICourseHealthService, CourseHealthService>();

        return services;
    }
}
