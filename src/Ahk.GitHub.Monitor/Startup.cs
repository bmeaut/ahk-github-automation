using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Ahk.GitHub.Monitor.Startup))]

namespace Ahk.GitHub.Monitor
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddMemoryCache(setup =>
            {
                setup.ExpirationScanFrequency = TimeSpan.FromMinutes(4);
            });
            builder.Services.AddSingleton<Services.IGitHubClientFactory, Services.GitHubClientFactory>();

            var eventHandlers = registerEventHandlers(builder.Services);
            builder.Services.AddSingleton<Services.IEventDispatchService, Services.EventDispatchService>(sp => new Services.EventDispatchService(sp, eventHandlers));

            var configuration = new ConfigurationBuilder().AddEnvironmentVariables("AHK_").Build();
            builder.Services.Configure<GitHubMonitorConfig>(configuration);
        }

        private static Services.EventDispatchConfigBuilder registerEventHandlers(IServiceCollection services)
        {
            return new Services.EventDispatchConfigBuilder(services)
                .Add<EventHandlers.BranchProtectionRuleHandler>(EventHandlers.BranchProtectionRuleHandler.GitHubWebhookEventName)
                .Add<EventHandlers.IssueCommentEditDeleteHandler>(EventHandlers.IssueCommentEditDeleteHandler.GitHubWebhookEventName)
                .Add<EventHandlers.PullRequestOpenDuplicateHandler>(EventHandlers.PullRequestOpenDuplicateHandler.GitHubWebhookEventName)
                .Add<EventHandlers.PullRequestReviewToAssigneeHandler>(EventHandlers.PullRequestReviewToAssigneeHandler.GitHubWebhookEventName)
                .Add<EventHandlers.GradeCommandHandler>(EventHandlers.GradeCommandHandler.GitHubWebhookEventName);
        }
    }
}