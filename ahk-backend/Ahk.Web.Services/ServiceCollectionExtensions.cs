using System.Net.Http.Headers;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.Courses;
using Ahk.Web.Services.GitHub;
using Ahk.Web.Services.GitHubWebhooks;
using Ahk.Web.Services.GitHubWebhooks.Handlers;
using Ahk.Web.Services.GitHubWebhooks.Handlers.GradeComment;
using Ahk.Web.Services.GitHubWebhooks.Handlers.StatusTracking;
using Ahk.Web.Services.Grading;
using Ahk.Web.Services.Health;
using Ahk.Web.Services.StatusTracking;
using Ahk.Web.Services.Submissions;
using Ahk.Web.Services.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Ahk.Web.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the domain services. All are scoped: they use the request's ApplicationDbContext.</summary>
    public static IServiceCollection AddAhkServices(this IServiceCollection services)
    {
        services.AddScoped<ISubmissionResolver, SubmissionResolver>();
        services.AddScoped<ISubmissionArchiveService, SubmissionArchiveService>();
        services.AddScoped<ISubmissionEventService, SubmissionEventService>();
        services.AddScoped<IStatusTrackingService, StatusTrackingService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<IGradeListingService, GradeListingService>();
        services.AddScoped<ICourseResolutionService, CourseResolutionService>();
        services.AddScoped<IWebhookTokenService, WebhookTokenService>();
        services.AddScoped<IPersonalAccessTokenService, PersonalAccessTokenService>();

        services.AddAhkGitHubApi();
        services.AddAhkGitHubWebhooks();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IAssignmentInviteService, AssignmentInviteService>();
        services.AddScoped<IStudentAssignmentService, StudentAssignmentService>();

        services.AddAhkCourseHealthChecks();

        return services;
    }

    /// <summary>
    /// The GitHub transport: Octokit clients for every API call, plus the per-course installation-token
    /// provider that authenticates them.
    ///
    /// The named <c>"github"</c> <see cref="HttpClient"/> registered here is *not* the API transport. It backs
    /// only <see cref="CourseGitHubAppTokenProvider"/>'s App-JWT bootstrap (signing a JWT and exchanging it for
    /// an installation token is not an API call, and converting it to Octokit would change the shape of the
    /// permissions the health check reads).
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
        services.AddScoped<ICourseGitHubClientFactory, CourseGitHubClientFactory>();
        services.AddScoped<IGitHubRepositoryService, GitHubRepositoryService>();

        return services;
    }

    /// <summary>
    /// The GitHub webhook receiver: the dispatcher plus every handler, ported from <c>github-monitor</c>.
    ///
    /// Registration order is the dispatch order and is kept identical to that app's
    /// <c>Startup.registerEventHandlers</c> — DI hands an <c>IEnumerable&lt;T&gt;</c> back in registration
    /// order, which is why the explicit config builder it used is not needed here.
    /// </summary>
    public static IServiceCollection AddAhkGitHubWebhooks(this IServiceCollection services)
    {
        services.AddScoped<IGitHubWebhookDispatcher, GitHubWebhookDispatcher>();
        services.AddScoped<IGitHubWebhookDeliveryProcessor, GitHubWebhookDeliveryProcessor>();

        services.AddScoped<IGitHubWebhookHandler, BranchProtectionRuleHandler>();
        services.AddScoped<IGitHubWebhookHandler, IssueCommentEditDeleteHandler>();
        services.AddScoped<IGitHubWebhookHandler, PullRequestOpenDuplicateHandler>();
        services.AddScoped<IGitHubWebhookHandler, PullRequestReviewToAssigneeHandler>();
        services.AddScoped<IGitHubWebhookHandler, GradeCommandIssueCommentHandler>();
        services.AddScoped<IGitHubWebhookHandler, GradeCommandReviewCommentHandler>();
        services.AddScoped<IGitHubWebhookHandler, ActionWorkflowRunHandler>();
        services.AddScoped<IGitHubWebhookHandler, BranchCreateStatusTrackingHandler>();
        services.AddScoped<IGitHubWebhookHandler, WorkflowRunStatusTrackingHandler>();
        services.AddScoped<IGitHubWebhookHandler, PullRequestStatusTrackingHandler>();

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
        services.AddScoped<ICourseHealthCheck, WebhookQueueHealthCheck>();
        services.AddScoped<ICourseHealthService, CourseHealthService>();

        // Singleton: it is the hand-off point between a request thread and the background refresh worker.
        services.AddSingleton<ICourseHealthRefreshQueue, CourseHealthRefreshQueue>();

        return services;
    }
}
