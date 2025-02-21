using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Config;
using Ahk.GitHub.Monitor.EventHandlers.BaseAndUtils;
using Ahk.GitHub.Monitor.Helpers;
using Ahk.GitHub.Monitor.Services.EventDispatch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.GitHub.Monitor;

public class GitHubMonitorFunction(
    IEventDispatchService eventDispatchService,
    ILogger<GitHubMonitorFunction> logger,
    IConfiguration configuration)
{
    [Function("github-webhook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
        HttpRequestData request)
    {
        request.Headers.TryGetValues("X-GitHub-Event", out IEnumerable<string> eventNameValues);
        var eventName = eventNameValues?.FirstOrDefault();
        request.Headers.TryGetValues("X-GitHub-Delivery", out IEnumerable<string> deliveryIdValues);
        var deliveryId = deliveryIdValues?.FirstOrDefault();
        request.Headers.TryGetValues("X-Hub-Signature-256", out IEnumerable<string> signatureValues);
        var receivedSignature = signatureValues?.FirstOrDefault();

        logger.LogInformation(
            "Webhook delivery: Delivery id = '{DeliveryId}', Event name = '{EventName}'",
            deliveryId, eventName);

        if (string.IsNullOrEmpty(eventName))
        {
            return new BadRequestObjectResult(new { error = "X-GitHub-Event header missing" });
        }

        if (string.IsNullOrEmpty(receivedSignature))
        {
            return new BadRequestObjectResult(new { error = "X-Hub-Signature-256 header missing" });
        }

        var requestBody = await request.ReadAsStringAsync();
        if (!PayloadParser<ActivityPayload>.TryParsePayload(requestBody, out ActivityPayload parsedRequestBody,
                out EventHandlerResult errorResult, logger))
        {
            return new BadRequestObjectResult(new { error = errorResult.Result });
        }

        var orgName = parsedRequestBody.Repository.Owner.Login;

        var orgConfig = new GitHubMonitorConfig();
        configuration.GetSection(GitHubMonitorConfig.GetSectionName(orgName)).Bind(orgConfig);

        if (string.IsNullOrEmpty(orgConfig.GitHubWebhookSecret))
        {
            return new ObjectResult(new { error = "GitHub secret not configured" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        if (string.IsNullOrEmpty(orgConfig.GitHubAppId) ||
            string.IsNullOrEmpty(orgConfig.GitHubAppPrivateKey))
        {
            return new ObjectResult(new { error = "GitHub App ID/Token not configured" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        if (!GitHubSignatureValidator.IsSignatureValid(requestBody, receivedSignature,
                orgConfig.GitHubWebhookSecret))
        {
            return new BadRequestObjectResult(new { error = "Payload signature not valid" });
        }

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
