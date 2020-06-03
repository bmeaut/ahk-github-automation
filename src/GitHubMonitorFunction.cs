using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor
{
    public static partial class GitHubMonitorFunction
    {
        [FunctionName("github-webhook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request,
            ILogger logger)
        {
            var githubSecret = Environment.GetEnvironmentVariable("AHK_GITHUB_SECRET", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(githubSecret))
            {
                return new ObjectResult(new { error = "GitHub secret not configured" }) { StatusCode = StatusCodes.Status500InternalServerError };
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AHK_GITHUB_APP_PRIVATE_KEY", EnvironmentVariableTarget.Process))
                || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AHK_GITHUB_APP_ID", EnvironmentVariableTarget.Process)))
            {
                return new ObjectResult(new { error = "GitHub App ID/Token not configured" }) { StatusCode = StatusCodes.Status500InternalServerError };
            }

            string eventName = request.Headers.GetValueOrDefault("X-GitHub-Event");
            string deliveryId = request.Headers.GetValueOrDefault("X-GitHub-Delivery");
            string receivedSignature = request.Headers.GetValueOrDefault("X-Hub-Signature");

            logger.LogInformation("Webhook delivery: Delivery id = '{DeliveryId}', Event name = '{EventName}'", deliveryId, eventName);

            var payload = new PayloadReader(request);
            if (!PayloadValidator.IsSignatureValid(payload.ReadAsByteArray(), receivedSignature, githubSecret))
            {
                return new BadRequestObjectResult(new { error = "Payload signature not valid" });
            }
            else
            {
                var webhookResult = new WebhookResult();
                try
                {
                    string requestBody = payload.ReadAsString();
                    switch (eventName)
                    {
                        case EventHandlers.BranchCreatedEventHandler.GitHubWebhookEventName:
                            await new EventHandlers.BranchCreatedEventHandler().Execute(requestBody, webhookResult);
                            break;
                        case EventHandlers.IssueCommentEventHandler.GitHubWebhookEventName:
                            await new EventHandlers.IssueCommentEventHandler().Execute(requestBody, webhookResult);
                            break;
                        case EventHandlers.PullRequestEventHandler.GitHubWebhookEventName:
                            await new EventHandlers.PullRequestEventHandler().Execute(requestBody, webhookResult);
                            break;
                        default:
                            webhookResult.LogInfo($"Event {eventName} is not of interrest");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    webhookResult.LogError(ex, "Failed to handle webhook");
                }

                return new OkObjectResult(webhookResult);
            }
        }
    }
}
