using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ahk.GitHub.Monitor
{
    public class GitHubMonitorFunction
    {
        private readonly IOptions<GitHubMonitorConfig> config;
        private readonly IGitHubClientFactory gitHubClientFactory;

        public GitHubMonitorFunction(IOptions<GitHubMonitorConfig> config, IGitHubClientFactory gitHubClientFactory)
        {
            this.config = config;
            this.gitHubClientFactory = gitHubClientFactory;
        }

        [FunctionName("github-webhook")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request,
            ILogger logger)
        {
            if (string.IsNullOrEmpty(config.Value.GitHubWebhookSecret))
                return new ObjectResult(new { error = "GitHub secret not configured" }) { StatusCode = StatusCodes.Status500InternalServerError };

            if (string.IsNullOrEmpty(config.Value.GitHubAppId) || string.IsNullOrEmpty(config.Value.GitHubAppPrivateKey))
                return new ObjectResult(new { error = "GitHub App ID/Token not configured" }) { StatusCode = StatusCodes.Status500InternalServerError };

            string eventName = request.Headers.GetValueOrDefault("X-GitHub-Event");
            string deliveryId = request.Headers.GetValueOrDefault("X-GitHub-Delivery");
            string receivedSignature = request.Headers.GetValueOrDefault("X-Hub-Signature");

            logger.LogInformation("Webhook delivery: Delivery id = '{DeliveryId}', Event name = '{EventName}'", deliveryId, eventName);

            var payload = new PayloadReader(request);
            if (!PayloadValidator.IsSignatureValid(await payload.ReadAsByteArray(), receivedSignature, config.Value.GitHubWebhookSecret))
            {
                return new BadRequestObjectResult(new { error = "Payload signature not valid" });
            }
            else
            {
                var webhookResult = new WebhookResult();
                try
                {
                    string requestBody = await payload.ReadAsString();
                    switch (eventName)
                    {
                        case EventHandlers.BranchCreatedEventHandler.GitHubWebhookEventName:
                            await new EventHandlers.BranchCreatedEventHandler(gitHubClientFactory).Execute(requestBody, webhookResult);
                            break;
                        case EventHandlers.IssueCommentEventHandler.GitHubWebhookEventName:
                            await new EventHandlers.IssueCommentEventHandler(gitHubClientFactory).Execute(requestBody, webhookResult);
                            break;
                        case EventHandlers.PullRequestEventHandler.GitHubWebhookEventName:
                            await new EventHandlers.PullRequestEventHandler(gitHubClientFactory).Execute(requestBody, webhookResult);
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
