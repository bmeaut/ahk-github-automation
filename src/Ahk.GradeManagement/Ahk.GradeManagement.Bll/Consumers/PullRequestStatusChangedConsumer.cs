using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Events;

using MassTransit;

using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Bll.Consumers;

public class PullRequestStatusChangedConsumer(
    ILogger<PullRequestStatusChangedConsumer> logger,
    AssignmentEventProcessorService assignmentEventProcessorService) : IConsumer<PullRequestStatusChanged>
{
    public async Task Consume(ConsumeContext<PullRequestStatusChanged> context)
    {
        var message = context.Message;
        logger.LogInformation("Pull request status changed event received for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
        await assignmentEventProcessorService.ConsumePullRequestStatusChangedEventAsync(message);
        logger.LogInformation("Pull request status changed event consumed for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
    }
}
