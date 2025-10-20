using Ahk.GitHub.Monitor.Config;
using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.EventHandlers.GradeComment;
using Ahk.GitHub.Monitor.Services.EventDispatch;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;

using MassTransit;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
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

builder.AddAzureKeyVaultConfiguration();

builder.Services.AddMassTransit(x =>
{
    x.UsingAzureServiceBus((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetValue<string>("Aspire:Azure:Messaging:ServiceBus:ahk-events:ConnectionString");
        cfg.Host(connectionString);
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

builder.Build().Run();

