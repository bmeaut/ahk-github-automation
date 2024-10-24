using System;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Helpers;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Services.EventDispatch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ahk.GitHub.Monitor
{
    public class GitHubMonitorFunction(
        IEventDispatchService eventDispatchService,
        IOptions<GitHubMonitorConfig> config,
        ILogger<GitHubMonitorFunction> logger)
    {
        [Function("github-webhook")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequestData request)
        {
            if (string.IsNullOrEmpty(config.Value.GitHubWebhookSecret))
            {
                return new ObjectResult(new { error = "GitHub secret not configured" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                };
            }

            if (string.IsNullOrEmpty(config.Value.GitHubAppId) ||
                string.IsNullOrEmpty(config.Value.GitHubAppPrivateKey))
            {
                return new ObjectResult(new { error = "GitHub App ID/Token not configured" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                };
            }

            request.Headers.TryGetValues("X-GitHub-Event", out var eventNameValues);
            string eventName = eventNameValues?.FirstOrDefault();
            request.Headers.TryGetValues("X-GitHub-Delivery", out var deliveryIdValues);
            string deliveryId = deliveryIdValues?.FirstOrDefault();
            request.Headers.TryGetValues("X-Hub-Signature-256", out var signatureValues);
            string receivedSignature = signatureValues?.FirstOrDefault();

            logger.LogInformation(
                "Webhook delivery: Delivery id = '{DeliveryId}', Event name = '{EventName}'",
                deliveryId, eventName);

            if (string.IsNullOrEmpty(eventName))
                return new BadRequestObjectResult(new { error = "X-GitHub-Event header missing" });
            if (string.IsNullOrEmpty(receivedSignature))
                return new BadRequestObjectResult(new { error = "X-Hub-Signature-256 header missing" });

            string requestBody = await request.ReadAsStringAsync();
            if (!GitHubSignatureValidator.IsSignatureValid(requestBody, receivedSignature,
                    config.Value.GitHubWebhookSecret))
                return new BadRequestObjectResult(new { error = "Payload signature not valid" });

            return await this.runCore(eventName, deliveryId, requestBody);
        }

        private async Task<IActionResult> runCore(string eventName, string deliveryId, string requestBody)
        {
            logger.LogInformation("Webhook delivery accepted with Delivery id = '{DeliveryId}'", deliveryId);
            var webhookResult = new WebhookResult();
            try
            {
                await eventDispatchService.Process(eventName, requestBody, webhookResult, logger);
                logger.LogInformation("Webhook delivery processed succesfully with Delivery id = '{DeliveryId}'",
                    deliveryId);
            }
            catch (Exception ex)
            {
                webhookResult.LogError(ex, "Failed to handle webhook");
                logger.LogError(ex, "github-webhook failed with Delivery id = '{DeliveryId}'", deliveryId);
            }

            return new OkObjectResult(webhookResult);
        }
    }
}
