using System.Text.Json;
using Ahk.Web.Data;
using Ahk.Web.Server.CourseContext;
using Ahk.Web.Services.Courses;
using Ahk.Web.Services.GitHub;
using Ahk.Web.Services.GitHubWebhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.Integrations;

/// <summary>
/// Receives GitHub App webhook deliveries — the entry point ported from <c>github-monitor</c>'s single Azure
/// Function. Anonymous by design: the <c>X-Hub-Signature-256</c> HMAC is the authentication, and there is no
/// fallback authorization policy in <c>Program.cs</c> for it to fight.
///
/// <para>Kept out of the OpenAPI document: the SPA never calls this, and NSwag would emit a TypeScript client
/// for a byte-exact signed payload that no browser can produce.</para>
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/integrations/github")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class GitHubWebhookController : ControllerBase
{
    /// <summary>GitHub caps webhook payloads at 25 MB. Bounds the work done before the signature is checked.</summary>
    private const int MaxPayloadBytes = 25 * 1024 * 1024;

    private readonly ICourseResolutionService courses;
    private readonly ICourseGitHubAppTokenProvider tokenProvider;
    private readonly ICourseGitHubClientFactory clientFactory;
    private readonly IGitHubWebhookDispatcher dispatcher;
    private readonly CurrentCourseProvider currentCourse;
    private readonly ApplicationDbContext db;
    private readonly ILogger<GitHubWebhookController> logger;

    public GitHubWebhookController(
        ICourseResolutionService courses,
        ICourseGitHubAppTokenProvider tokenProvider,
        ICourseGitHubClientFactory clientFactory,
        IGitHubWebhookDispatcher dispatcher,
        CurrentCourseProvider currentCourse,
        ApplicationDbContext db,
        ILogger<GitHubWebhookController> logger)
    {
        this.courses = courses;
        this.tokenProvider = tokenProvider;
        this.clientFactory = clientFactory;
        this.dispatcher = dispatcher;
        this.currentCourse = currentCourse;
        this.db = db;
        this.logger = logger;
    }

    /// <summary>
    /// Deliberately no <c>[Consumes]</c>: a webhook mistakenly configured as <c>x-www-form-urlencoded</c>
    /// should get our explanatory 400 in the delivery log, not a bare framework 415.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxPayloadBytes)]
    [ProducesResponseType(typeof(WebhookResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var eventName = Request.Headers["X-GitHub-Event"].FirstOrDefault();
        var deliveryId = Request.Headers["X-GitHub-Delivery"].FirstOrDefault();
        var receivedSignature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();

        logger.LogInformation(
            "Webhook delivery: Delivery id = '{DeliveryId}', Event name = '{EventName}'", deliveryId, eventName);

        if (string.IsNullOrEmpty(eventName))
            return BadRequest(new { error = "X-GitHub-Event header missing" });

        if (string.IsNullOrEmpty(receivedSignature))
            return BadRequest(new { error = "X-Hub-Signature-256 header missing" });

        var requestBody = await RawBody.ReadAsync(Request, cancellationToken);
        if (string.IsNullOrEmpty(requestBody))
            return BadRequest(new { error = "request body was empty" });

        // The secret is per course, and the only thing in a delivery that identifies the course is the
        // repository name inside the body — so an untrusted body must be parsed before it can be verified.
        // Everything up to the signature check is therefore kept inert: one property is read and the document
        // is dropped, and nothing is written, logged or called until the HMAC passes.
        var repositoryFullName = TryReadRepositoryFullName(requestBody);
        if (string.IsNullOrEmpty(repositoryFullName))
            return BadRequest(new { error = "no repository information in webhook payload" });

        var course = await courses.ResolveByRepositoryAsync(repositoryFullName, cancellationToken);
        if (course is null)
        {
            // 202 rather than 4xx: during cutover an organization legitimately contains repositories that are
            // not a course, and a delivery log full of red trains administrators to stop reading it.
            logger.LogInformation("Webhook delivery for '{Repository}' matches no course", repositoryFullName);
            return Accepted(new { message = $"repository '{repositoryFullName}' is not mapped to a course" });
        }

        var config = await db.CourseGitHubConfigs.AsNoTracking()
            .FirstOrDefaultAsync(g => g.CourseId == course.Id, cancellationToken);

        if (config is null)
            return Accepted(new { message = $"GitHub integration is not configured for course '{course.Slug}'" });

        if (!config.Enabled)
            return Accepted(new { message = $"GitHub integration is turned off for course '{course.Slug}'" });

        if (string.IsNullOrEmpty(config.GitHubWebhookSecret))
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "GitHub secret not configured" });

        if (!GitHubSignatureValidator.IsSignatureValid(requestBody, receivedSignature, config.GitHubWebhookSecret))
            return BadRequest(new { error = "Payload signature not valid" });

        // Past this point the body is trusted.
        currentCourse.Set(course.Id);

        // The org-based lookup is used rather than the payload's installation.id: the course is resolved *by*
        // organization, so the two are the same installation by construction, and deriving a credential from a
        // payload field is a weaker path to the same answer.
        var token = await tokenProvider.GetForCourseAsync(course.Id, bypassCache: false, cancellationToken);
        if (token is null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "GitHub App ID/Token not configured" });

        var context = new GitHubWebhookContext
        {
            CourseId = course.Id,
            GitHubEventName = eventName,
            DeliveryId = deliveryId ?? string.Empty,
            RequestBody = requestBody,
            GitHubClient = clientFactory.CreateForToken(token.Token),
            WorkflowRunThreshold = config.WorkflowRunThreshold,
        };

        logger.LogInformation("Webhook delivery accepted with Delivery id = '{DeliveryId}'", deliveryId);

        var webhookResult = new WebhookResult();
        try
        {
            await dispatcher.ProcessAsync(context, webhookResult, cancellationToken);
            logger.LogInformation("Webhook delivery processed successfully with Delivery id = '{DeliveryId}'", deliveryId);
        }
#pragma warning disable CA1031 // A handled delivery still answers 200; the failure is reported in the body.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            webhookResult.LogError(ex, "Failed to handle webhook");
            logger.LogError(ex, "github-webhook failed with Delivery id = '{DeliveryId}'", deliveryId);
        }

        return Ok(webhookResult);
    }

    /// <summary>
    /// Reads <c>repository.full_name</c> and nothing else out of an as-yet-unverified body. Returns null on any
    /// malformed input — this runs before authentication, so it must not throw.
    /// </summary>
    private static string? TryReadRepositoryFullName(string requestBody)
    {
        try
        {
            using var document = JsonDocument.Parse(requestBody);

            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("repository", out var repository)
                || repository.ValueKind != JsonValueKind.Object
                || !repository.TryGetProperty("full_name", out var fullName))
            {
                return null;
            }

            return fullName.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
