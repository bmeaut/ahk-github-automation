using System.Text.Json;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Courses;
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
/// <para>The delivery is verified and recorded here, and answered 202 straight away;
/// <see cref="GitHubWebhookDeliveryWorker"/> does the work. GitHub gives a delivery <strong>ten
/// seconds</strong>, and a single <c>pull_request</c> event costs the handlers one GitHub API call per closed
/// pull request in the repository — processing inline was a race this endpoint kept losing, and losing it
/// mid-fan-out left a merged pull request with no grade recorded against it.</para>
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
    private readonly ApplicationDbContext db;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<GitHubWebhookController> logger;

    public GitHubWebhookController(
        ICourseResolutionService courses,
        ApplicationDbContext db,
        TimeProvider timeProvider,
        ILogger<GitHubWebhookController> logger)
    {
        this.courses = courses;
        this.db = db;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    /// <summary>
    /// Deliberately no <c>[Consumes]</c>: a webhook mistakenly configured as <c>x-www-form-urlencoded</c>
    /// should get our explanatory 400 in the delivery log, not a bare framework 415.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxPayloadBytes)]
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

        // Past this point the body is trusted, and the only thing left to do is record it.
        var now = timeProvider.GetUtcNow();
        var delivery = new GitHubWebhookDelivery
        {
            CourseId = course.Id,
            DeliveryId = string.IsNullOrEmpty(deliveryId) ? null : deliveryId,
            EventName = eventName,
            RepositoryFullName = repositoryFullName,
            Payload = requestBody,
            ReceivedAt = now,
            Status = GitHubWebhookDeliveryStatus.Pending,
            NextAttemptAt = now,
        };

        db.GitHubWebhookDeliveries.Add(delivery);

        try
        {
            // CancellationToken.None, not the bound RequestAborted: this insert is the single durable act of
            // the request. GitHub hangs up at its own ten-second deadline, and cancelling here would leave a
            // delivery neither processed nor recorded — the one outcome with no way back. SqlClient's own
            // command timeout is the real bound.
            await db.SaveChangesAsync(CancellationToken.None);
        }
#pragma warning disable CA1031 // Any failure to record must be reported as one, not swallowed into a 202.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // 500, deliberately: nothing was persisted and nothing ran, so a red entry in the delivery log is
            // the truth — and it keeps GitHub's own Redeliver button as the way back.
            logger.LogError(ex, "Failed to queue webhook delivery '{DeliveryId}'", deliveryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to record the delivery" });
        }

        logger.LogInformation(
            "Webhook delivery queued: Delivery id = '{DeliveryId}', Event name = '{EventName}', Queue id = {QueueId}",
            deliveryId, eventName, delivery.Id);

        return Accepted(new
        {
            status = "queued",
            deliveryId,
            queueId = delivery.Id,
            message = "queued for processing; see Site administration → Webhook deliveries",
        });
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
