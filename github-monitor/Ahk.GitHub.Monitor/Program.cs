#pragma warning disable CA1506
using System;
using Ahk.GitHub.Monitor;
using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.EventHandlers.GradeComment;
using Ahk.GitHub.Monitor.EventHandlers.StatusTracking;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Services.AzureQueues;
using Ahk.GitHub.Monitor.Services.EventDispatch;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;
using Ahk.GitHub.Monitor.Services.GradeStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore;
using Azure.Core;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // Application Insights setup
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Load configuration from environment variables with the prefix "AHK_"
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables("AHK_")
            .Build();

        services.AddLogging(); // Register logging services

        // Add memory cache with a specific expiration scan frequency
        services.AddMemoryCache(setup =>
        {
            setup.ExpirationScanFrequency = TimeSpan.FromMinutes(4);
        });

        // Register services
        services
            .AddSingleton<IGitHubClientFactory,
                GitHubClientFactory>();

        RegisterEventHandlers(services);
        services
            .AddSingleton<IEventDispatchService,
                EventDispatchService>();

        // Bind configuration
        services.Configure<GitHubMonitorConfig>(configuration);

        // Add Azure Queue integration based on configuration
        AddAzureQueueIntegration(services, configuration);
    })
    .Build();

host.Run();

void RegisterEventHandlers(IServiceCollection services)
{
    var builder = new EventDispatchConfigBuilder(services)
        .Add<BranchProtectionRuleHandler>(BranchProtectionRuleHandler.GitHubWebhookEventName)
        .Add<IssueCommentEditDeleteHandler>(IssueCommentEditDeleteHandler.GitHubWebhookEventName)
        .Add<PullRequestOpenDuplicateHandler>(PullRequestOpenDuplicateHandler.GitHubWebhookEventName)
        .Add<PullRequestReviewToAssigneeHandler>(PullRequestReviewToAssigneeHandler.GitHubWebhookEventName)
        .Add<GradeCommandIssueCommentHandler>(GradeCommandIssueCommentHandler.GitHubWebhookEventName)
        .Add<GradeCommandReviewCommentHandler>(GradeCommandReviewCommentHandler.GitHubWebhookEventName)
        .Add<ActionWorkflowRunHandler>(ActionWorkflowRunHandler.GitHubWebhookEventName)
        .Add<RepositoryCreateStatusTrackingHandler>(RepositoryCreateStatusTrackingHandler.GitHubWebhookEventName)
        .Add<PullRequestStatusTrackingHandler>(PullRequestStatusTrackingHandler.GitHubWebhookEventName);
    var config = builder.Build();
    services.AddSingleton(config);
}

void AddAzureQueueIntegration(IServiceCollection services, IConfiguration configuration)
{
    var config = configuration.Get<GitHubMonitorConfig>();

    if (!string.IsNullOrEmpty(config?.EventsQueueConnectionString))
    {
        services
            .AddSingleton<IGradeStore, GradeStoreAzureQueue>();
        services
            .AddSingleton<IStatusTrackingStore,
                StatusTrackingStoreAzureQueue>();

        services.AddAzureClients(az =>
        {
            az.ConfigureDefaults(opts => opts.Diagnostics.IsLoggingEnabled = false);
            az.AddQueueServiceClient(config.EventsQueueConnectionString)
                .WithName(QueueClientName.Name)
                .ConfigureOptions(options =>
                {
                    options.MessageEncoding = QueueMessageEncoding.Base64;
                    options.Retry.Mode = RetryMode.Exponential;
                    options.Retry.MaxRetries = 5;
                    options.Retry.MaxDelay = TimeSpan.FromSeconds(100);
                });
        });
    }
    else
    {
        services.AddSingleton<IGradeStore, GradeStoreNoop>();
        services
            .AddSingleton<IStatusTrackingStore,
                StatusTrackingStoreNoop>();
    }
}
#pragma warning restore CA1506
