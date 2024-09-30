using System;
using Ahk.GitHub.Monitor;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) => {
        // Application Insights setup
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Load configuration from environment variables with the prefix "AHK_"
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables("AHK_")
            .Build();

        // Add memory cache with a specific expiration scan frequency
        services.AddMemoryCache(setup =>
        {
            setup.ExpirationScanFrequency = TimeSpan.FromMinutes(4);
        });

        // Register services
        services.AddSingleton<Ahk.GitHub.Monitor.Services.IGitHubClientFactory, Ahk.GitHub.Monitor.Services.GitHubClientFactory>();

        registerEventHandlers(services);
        services.AddSingleton<Ahk.GitHub.Monitor.Services.IEventDispatchService, Ahk.GitHub.Monitor.Services.EventDispatchService>();

        // Bind configuration
        services.Configure<GitHubMonitorConfig>(configuration);

        // Add Azure Queue integration based on configuration
        addAzureQueueIntegration(services, configuration);
    })
    .Build();

host.Run();

void registerEventHandlers(IServiceCollection services)
{
    var builder = new Ahk.GitHub.Monitor.Services.EventDispatchConfigBuilder(services)
        .Add<Ahk.GitHub.Monitor.EventHandlers.BranchProtectionRuleHandler>(Ahk.GitHub.Monitor.EventHandlers.BranchProtectionRuleHandler.GitHubWebhookEventName)
        .Add<Ahk.GitHub.Monitor.EventHandlers.IssueCommentEditDeleteHandler>(Ahk.GitHub.Monitor.EventHandlers.IssueCommentEditDeleteHandler.GitHubWebhookEventName)
        .Add<Ahk.GitHub.Monitor.EventHandlers.PullRequestOpenDuplicateHandler>(Ahk.GitHub.Monitor.EventHandlers.PullRequestOpenDuplicateHandler.GitHubWebhookEventName)
        .Add<Ahk.GitHub.Monitor.EventHandlers.PullRequestReviewToAssigneeHandler>(Ahk.GitHub.Monitor.EventHandlers.PullRequestReviewToAssigneeHandler.GitHubWebhookEventName)
        .Add<Ahk.GitHub.Monitor.EventHandlers.GradeCommandIssueCommentHandler>(Ahk.GitHub.Monitor.EventHandlers.GradeCommandIssueCommentHandler.GitHubWebhookEventName)
        .Add<Ahk.GitHub.Monitor.EventHandlers.GradeCommandReviewCommentHandler>(Ahk.GitHub.Monitor.EventHandlers.GradeCommandReviewCommentHandler.GitHubWebhookEventName)
        .Add<Ahk.GitHub.Monitor.EventHandlers.ActionWorkflowRunHandler>(Ahk.GitHub.Monitor.EventHandlers.ActionWorkflowRunHandler.GitHubWebhookEventName)
        .Add<Ahk.GitHub.Monitor.EventHandlers.BranchCreateStatusTrackingHandler>(Ahk.GitHub.Monitor.EventHandlers.BranchCreateStatusTrackingHandler.GitHubWebhookEventName)
        .Add<Ahk.GitHub.Monitor.EventHandlers.WorkflowRunStatusTrackingHandler>(Ahk.GitHub.Monitor.EventHandlers.WorkflowRunStatusTrackingHandler.GitHubWebhookEventName)
        .Add<Ahk.GitHub.Monitor.EventHandlers.PullRequestStatusTrackingHandler>(Ahk.GitHub.Monitor.EventHandlers.PullRequestStatusTrackingHandler.GitHubWebhookEventName);
    var config = builder.Build();
    services.AddSingleton(config);
}

void addAzureQueueIntegration(IServiceCollection services, IConfiguration configuration)
{
    var config = configuration.Get<GitHubMonitorConfig>();

    if (!string.IsNullOrEmpty(config?.EventsQueueConnectionString))
    {
        services.AddSingleton<Ahk.GitHub.Monitor.Services.IGradeStore, Ahk.GitHub.Monitor.Services.GradeStoreAzureQueue>();
        services.AddSingleton<Ahk.GitHub.Monitor.Services.IStatusTrackingStore, Ahk.GitHub.Monitor.Services.StatusTrackingStoreAzureQueue>();

        services.AddAzureClients(az =>
        {
            az.ConfigureDefaults(opts => opts.Diagnostics.IsLoggingEnabled = false);
            az.AddQueueServiceClient(config.EventsQueueConnectionString)
                .WithName(Ahk.GitHub.Monitor.Services.AzureQueues.QueueClientName.Name)
                .ConfigureOptions(options =>
                {
                    options.MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64;
                    options.Retry.Mode = Azure.Core.RetryMode.Exponential;
                    options.Retry.MaxRetries = 5;
                    options.Retry.MaxDelay = TimeSpan.FromSeconds(100);
                });
        });
    }
    else
    {
        services.AddSingleton<Ahk.GitHub.Monitor.Services.IGradeStore, Ahk.GitHub.Monitor.Services.GradeStoreNoop>();
        services.AddSingleton<Ahk.GitHub.Monitor.Services.IStatusTrackingStore, Ahk.GitHub.Monitor.Services.StatusTrackingStoreNoop>();
    }
}
