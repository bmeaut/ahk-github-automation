using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Events;

using MassTransit;

using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Bll.Consumers;

public class AssignmentAcceptedConsumer(
    ILogger<AssignmentAcceptedConsumer> logger,
    AssignmentEventProcessorService assignmentEventProcessorService) : IConsumer<AssignmentAccepted>
{
    public async Task Consume(ConsumeContext<AssignmentAccepted> context)
    {
        var message = context.Message;
        logger.LogInformation("Assignment accepted event received for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
        await assignmentEventProcessorService.ConsumeAssignmentAcceptedEventAsync(message);
        logger.LogInformation("Assignment accepted event consumed for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
    }
}
