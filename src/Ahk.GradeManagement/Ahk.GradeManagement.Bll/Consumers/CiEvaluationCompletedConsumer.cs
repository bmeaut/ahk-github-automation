using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Events;

using MassTransit;

using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Bll.Consumers;

public class CiEvaluationCompletedConsumer(
    ILogger<CiEvaluationCompletedConsumer> logger,
    AssignmentEventProcessorService assignmentEventProcessorService) : IConsumer<CiEvaluationCompleted>
{
    public async Task Consume(ConsumeContext<CiEvaluationCompleted> context)
    {
        var message = context.Message;
        logger.LogInformation("CI evaluation completed event received for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
        await assignmentEventProcessorService.ConsumeCiEvaluationCompletedEventAsync(message);
        logger.LogInformation("CI evaluation completed event consumed for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
    }
}
