using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Events;

using MassTransit;

using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Bll.Consumers;

public class PullRequestOpenedConsumer(
    ILogger<PullRequestOpenedConsumer> logger,
    AssignmentEventProcessorService assignmentEventProcessorService) : IConsumer<PullRequestOpened>
{
    public async Task Consume(ConsumeContext<PullRequestOpened> context)
    {
        var message = context.Message;
        logger.LogInformation("Pull request opened event received for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
        await assignmentEventProcessorService.ConsumePullRequestOpenedEventAsync(message);
        logger.LogInformation("Pull request opened event consumed for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
    }
}
