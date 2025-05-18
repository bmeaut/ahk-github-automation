using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Config;
using Ahk.GitHub.Monitor.EventHandlers.BaseAndUtils;
using Ahk.GitHub.Monitor.Helpers;
using Ahk.GitHub.Monitor.Services.EventDispatch;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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
        string keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
        if (keyVaultUrl == null)
        {
            return new BadRequestObjectResult(new { error = "Please set environment variable KEY_VAULT_URI" });
        }
        var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

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

        if (eventName == "ping")
        {
            return new OkObjectResult("pong");
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

        var githubAppId = await secretClient.GetSecretAsync($"GitHubMonitorConfig--{orgName}--GitHubAppId");
        var githubAppPrivateKey = await secretClient.GetSecretAsync($"GitHubMonitorConfig--{orgName}--GitHubAppPrivateKey");
        var githubWebhookSecret = await secretClient.GetSecretAsync($"GitHubMonitorConfig--{orgName}--GitHubWebhookSecret");


        if (string.IsNullOrEmpty(githubWebhookSecret.Value.Value))
        {
            return new ObjectResult(new { error = "GitHub secret not configured" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        if (string.IsNullOrEmpty(githubAppId.Value.Value) ||
            string.IsNullOrEmpty(githubAppPrivateKey.Value.Value))
        {
            return new ObjectResult(new { error = "GitHub App ID/Token not configured" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        if (!GitHubSignatureValidator.IsSignatureValid(requestBody, receivedSignature,
                githubWebhookSecret.Value.Value))
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
