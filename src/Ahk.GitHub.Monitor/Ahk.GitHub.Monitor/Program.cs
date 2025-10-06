using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.EventHandlers.GradeComment;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Services.AzureQueues;
using Ahk.GitHub.Monitor.Services.EventDispatch;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;
using Ahk.GitHub.Monitor.Services.GradeStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore;

using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Queues;

using AzureKeyVaultEmulator.Aspire.Client;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;


var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var kvConnString = builder.Configuration.GetConnectionString("KeyVault");
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAzureKeyVaultEmulator(kvConnString);
    builder.Configuration.AddAzureKeyVault(
        new SecretClient(new Uri(kvConnString), new EmulatedTokenCredential(kvConnString), new()
        {
            DisableChallengeResourceVerification = true
        }),
        new AzureKeyVaultConfigurationOptions());
}
else
{
    builder.Services.AddAzureClients(client =>
    {
        client.AddSecretClient(new Uri(kvConnString));
    });

    builder.Configuration.AddAzureKeyVault(new Uri(kvConnString), new DefaultAzureCredential());
}

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
    .Add<BranchProtectionRuleHandler>(BranchProtectionRuleHandler.GitHubWebhookEventName)
    .Add<IssueCommentEditDeleteHandler>(IssueCommentEditDeleteHandler.GitHubWebhookEventName)
    .Add<PullRequestOpenDuplicateHandler>(PullRequestOpenDuplicateHandler.GitHubWebhookEventName)
    .Add<PullRequestReviewToAssigneeHandler>(PullRequestReviewToAssigneeHandler.GitHubWebhookEventName)
    .Add<GradeCommandIssueCommentHandler>(GradeCommandIssueCommentHandler.GitHubWebhookEventName)
    .Add<GradeCommandReviewCommentHandler>(GradeCommandReviewCommentHandler.GitHubWebhookEventName)
    .Add<ActionWorkflowRunHandler>(ActionWorkflowRunHandler.GitHubWebhookEventName)
    .Add<RepositoryCreateStatusTrackingHandler>(RepositoryCreateStatusTrackingHandler.GitHubWebhookEventName)
    .Add<PullRequestStatusTrackingHandler>(PullRequestStatusTrackingHandler.GitHubWebhookEventName);
builder.Services.AddSingleton(eventDispatchBuilder.Build());

builder.Services.AddSingleton<IGradeStore, GradeStoreAzureQueue>();
builder.Services.AddSingleton<IStatusTrackingStore, StatusTrackingStoreAzureQueue>();

builder.Build().Run();

