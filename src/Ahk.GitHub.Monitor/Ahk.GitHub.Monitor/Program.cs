using Ahk.GitHub.Monitor.Config;
using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.EventHandlers.GradeComment;
using Ahk.GitHub.Monitor.Services.AzureQueues;
using Ahk.GitHub.Monitor.Services.EventDispatch;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;
using Ahk.GitHub.Monitor.Services.GradeStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore;

using Azure.Core;
using Azure.Storage.Queues;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;


var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.AddAzureKeyVaultConfiguration();

builder.Services.AddAzureClients(client =>
{
    client.AddQueueServiceClient(builder.Configuration["ahk-queue-storage"])
        .WithName(QueueClientName.Name)
        .ConfigureOptions(options =>
        {
            options.MessageEncoding = QueueMessageEncoding.Base64;
            options.Retry.Mode = RetryMode.Exponential;
            options.Retry.MaxRetries = 5;
            options.Retry.MaxDelay = TimeSpan.FromSeconds(100);
        });
});

builder.Services.AddMemoryCache(setup => setup.ExpirationScanFrequency = TimeSpan.FromMinutes(4));

builder.Services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();
builder.Services.AddSingleton<IEventDispatchService, EventDispatchService>();
var eventDispatchBuilder = new EventDispatchConfigBuilder(builder.Services)
    .Add<BranchProtectionRuleHandler>()
    .Add<IssueCommentEditDeleteHandler>()
    .Add<PullRequestOpenDuplicateHandler>()
    .Add<PullRequestReviewToAssigneeHandler>()
    .Add<GradeCommandIssueCommentHandler>()
    .Add<GradeCommandReviewCommentHandler>()
    .Add<ActionWorkflowRunHandler>()
    .Add<RepositoryCreatedStatusTrackingHandler>()
    .Add<PullRequestStatusTrackingHandler>();
builder.Services.AddSingleton(eventDispatchBuilder.Build());

builder.Services.AddSingleton<IGradeStore, GradeStoreAzureQueue>();
builder.Services.AddSingleton<IStatusTrackingStore, StatusTrackingStoreAzureQueue>();

builder.Build().Run();

