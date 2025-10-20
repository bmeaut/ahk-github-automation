using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Events;

using MassTransit;

using Microsoft.Extensions.Logging;

namespace Ahk.GradeManagement.Bll.Consumers;

public class AssignmentGradedByTeacherConsumer(
    ILogger<AssignmentGradedByTeacherConsumer> logger,
    AssignmentEventProcessorService assignmentEventProcessorService) : IConsumer<AssignmentGradedByTeacher>
{
    public async Task Consume(ConsumeContext<AssignmentGradedByTeacher> context)
    {
        var message = context.Message;
        logger.LogInformation("Assignment graded by teacher event received for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
        await assignmentEventProcessorService.ConsumeAssignmentGradedByTeacherEventAsync(message);
        logger.LogInformation("Assignment graded by teacher event consumed for repo url: {RepoUrl}", message.GitHubRepositoryUrl);
    }
}
