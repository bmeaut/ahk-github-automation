using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Events;

using MassTransit;

using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Bll.Consumers;

public class TeacherAssignedConsumer(
    ILogger<TeacherAssignedConsumer> logger,
    AssignmentEventProcessorService assignmentEventProcessorService) : IConsumer<TeacherAssigned>
{
    public async Task Consume(ConsumeContext<TeacherAssigned> context)
    {
        var message = context.Message;
        logger.LogInformation("Teacher assigned event received for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
        await assignmentEventProcessorService.ConsumeTeacherAssignedEventAsync(message);
        logger.LogInformation("Teacher assigned event consumed for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
    }
}
