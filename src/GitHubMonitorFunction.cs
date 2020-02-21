using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
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
            var webhookResult = new WebhookResult();
            if (!GitHubClientHelper.TryCreateGitHubClient(out var gitHubClient))
            {
                webhookResult.LogError("AHK_GITHUB_TOKEN settings not provided");
            }
            else
            {
                string eventName = request.Headers.GetValueOrDefault("X-GitHub-Event");
                string deliveryId = request.Headers.GetValueOrDefault("X-GitHub-Delivery");
                // string signature = request.Headers.GetValueOrDefault("X-Hub-Signature");

                logger.LogInformation("Webhook delivery: Delivery id = '{DeliveryId}', Event name = '{EventName}'", deliveryId, eventName);

                string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
                try
                {
                    switch (eventName)
                    {
                        case EventHandlers.BranchCreatedEventHandler.GitHubWebhookEventName:
                            await new EventHandlers.BranchCreatedEventHandler(gitHubClient).Execute(requestBody, webhookResult);
                            break;
                        case EventHandlers.IssueEventHandler.GitHubWebhookEventName:
                            await new EventHandlers.IssueEventHandler(gitHubClient).Execute(requestBody, webhookResult);
                            break;
                        default:
                            webhookResult.LogInfo($"Event {eventName} is none of interrest");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    webhookResult.LogError(ex, "Failed to handle webhook");
                }
            }

            return new OkObjectResult(webhookResult);
        }
    }
}
