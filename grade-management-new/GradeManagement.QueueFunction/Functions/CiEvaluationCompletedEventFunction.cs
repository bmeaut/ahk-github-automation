using GradeManagement.Bll;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GradeManagement.QueueFunction.Functions;

public class CiEvaluationCompletedEventFunction
{
    private readonly ILogger<CiEvaluationCompletedEventFunction> _logger;
    private readonly AssignmentEventProcessorService _assignmentEventProcessorService;

    public CiEvaluationCompletedEventFunction(ILogger<CiEvaluationCompletedEventFunction> logger,
        AssignmentEventProcessorService assignmentEventProcessorService)
    {
        _logger = logger;
        _assignmentEventProcessorService = assignmentEventProcessorService;
    }

    [Function(nameof(CiEvaluationCompletedEventFunction))]
    public async Task Run(
        [QueueTrigger("ci-evaluation-completed", Connection = "AHK_EventsQueueConnectionString")]
        CiEvaluationCompleted ciEvaluationCompleted)
    {
        _logger.LogInformation(
            $"CI evaluation completed event function triggered for repo url: {ciEvaluationCompleted.GitHubRepositoryUrl}");
        await _assignmentEventProcessorService.ConsumeCiEvaluationCompletedEventAsync(ciEvaluationCompleted);
        _logger.LogInformation(
            $"CI evaluation completed event consumed for repo url: {ciEvaluationCompleted.GitHubRepositoryUrl}");
    }
}
