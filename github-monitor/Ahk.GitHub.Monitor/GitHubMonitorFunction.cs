using Ahk.GitHub.Monitor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor
{
    public class GitHubMonitorFunction
    {
        private readonly IEventDispatchService eventDispatchService;
        private readonly IOptions<GitHubMonitorConfig> config;

        public GitHubMonitorFunction(IEventDispatchService eventDispatchService, IOptions<GitHubMonitorConfig> config)
        {
            this.eventDispatchService = eventDispatchService;
            this.config = config;
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
            string receivedSignature = request.Headers.GetValueOrDefault("X-Hub-Signature-256");

            logger.LogInformation("Webhook delivery: Delivery id = '{DeliveryId}', Event name = '{EventName}'", deliveryId, eventName);

            if (string.IsNullOrEmpty(eventName))
                return new BadRequestObjectResult(new { error = "X-GitHub-Event header missing" });
            if (string.IsNullOrEmpty(receivedSignature))
                return new BadRequestObjectResult(new { error = "X-Hub-Signature-256 header missing" });

            string requestBody = await request.ReadAsStringAsync();
            if (!GitHubSignatureValidator.IsSignatureValid(requestBody, receivedSignature, config.Value.GitHubWebhookSecret))
                return new BadRequestObjectResult(new { error = "Payload signature not valid" });

            return await runCore(logger, eventName, deliveryId, requestBody);
        }

        private async Task<IActionResult> runCore(ILogger logger, string eventName, string deliveryId, string requestBody)
        {
            logger.LogInformation("Webhook delivery accepted with Delivery id = '{DeliveryId}'", deliveryId);
            var webhookResult = new WebhookResult();
            try
            {
                await eventDispatchService.Process(eventName, requestBody, webhookResult, logger);
                logger.LogInformation("Webhook delivery processed succesfully with Delivery id = '{DeliveryId}'", deliveryId);
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
