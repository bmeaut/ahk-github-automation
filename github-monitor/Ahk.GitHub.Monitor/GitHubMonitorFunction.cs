using System;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ahk.GitHub.Monitor
{
    public class GitHubMonitorFunction
    {
        private readonly IEventDispatchService eventDispatchService;
        private readonly IOptions<GitHubMonitorConfig> config;
        private readonly ILogger<GitHubMonitorFunction> logger;

        public GitHubMonitorFunction(IEventDispatchService eventDispatchService, IOptions<GitHubMonitorConfig> config, ILogger<GitHubMonitorFunction> logger)
        {
            this.eventDispatchService = eventDispatchService;
            this.config = config;
            this.logger = logger;
        }

        [Function("github-webhook")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData  request)
        {
            if (string.IsNullOrEmpty(config.Value.GitHubWebhookSecret))
                return new ObjectResult(new { error = "GitHub secret not configured" }) { StatusCode = StatusCodes.Status500InternalServerError };

            if (string.IsNullOrEmpty(config.Value.GitHubAppId) || string.IsNullOrEmpty(config.Value.GitHubAppPrivateKey))
                return new ObjectResult(new { error = "GitHub App ID/Token not configured" }) { StatusCode = StatusCodes.Status500InternalServerError };

            string eventName = request.Headers.GetValues("X-GitHub-Event").FirstOrDefault();
            string deliveryId = request.Headers.GetValues("X-GitHub-Delivery").FirstOrDefault();
            string receivedSignature = request.Headers.GetValues("X-Hub-Signature-256").FirstOrDefault();

            logger.LogInformation("Webhook delivery: Delivery id = '{DeliveryId}', Event name = '{EventName}'", deliveryId, eventName);

            if (string.IsNullOrEmpty(eventName))
                return new BadRequestObjectResult(new { error = "X-GitHub-Event header missing" });
            if (string.IsNullOrEmpty(receivedSignature))
                return new BadRequestObjectResult(new { error = "X-Hub-Signature-256 header missing" });

            string requestBody = await request.ReadAsStringAsync();
            if (!GitHubSignatureValidator.IsSignatureValid(requestBody, receivedSignature, config.Value.GitHubWebhookSecret))
                return new BadRequestObjectResult(new { error = "Payload signature not valid" });

            return await runCore(eventName, deliveryId, requestBody);
        }

        private async Task<IActionResult> runCore(string eventName, string deliveryId, string requestBody)
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
