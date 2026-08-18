using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Admin.Dto;
using Ahk.Web.Services.GitHubWebhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.Admin;

/// <summary>
/// The webhook delivery log.
///
/// <para>This replaces something we gave up deliberately. The receiver used to answer 200 with a per-handler
/// message list, and an administrator read that in the GitHub App's <em>Advanced → Recent Deliveries</em> tab.
/// Now that a delivery is answered 202 before any handler runs, that body can no longer say anything — so the
/// outcomes are recorded against the delivery and shown here instead, along with the one thing GitHub's own
/// view never had: a re-run that skips the handlers which already worked.</para>
///
/// <para>Host/admin context, so there is no <c>{course}</c> segment and no current course.
/// <see cref="GitHubWebhookDelivery"/> carries no query filter, which is what lets these reads work at all —
/// they filter on <c>CourseId</c> themselves.</para>
/// </summary>
[ApiController]
[Route("api/admin/webhook-deliveries")]
[Authorize(Roles = Roles.Admin)]
public sealed class WebhookDeliveriesAdminController : ControllerBase
{
    /// <summary>Bounded so a course with a long history cannot be asked for in one request.</summary>
    private const int MaxPageSize = 100;

    private readonly ApplicationDbContext db;
    private readonly TimeProvider timeProvider;

    public WebhookDeliveriesAdminController(ApplicationDbContext db, TimeProvider timeProvider)
    {
        this.db = db;
        this.timeProvider = timeProvider;
    }

    /// <summary>
    /// A page of deliveries, newest first, plus the 24-hour tallies the summary tiles show — one request, so
    /// the tiles and the table cannot disagree with each other.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(WebhookDeliveryListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WebhookDeliveryListDto>> List(
        [FromQuery] int? courseId,
        [FromQuery] GitHubWebhookDeliveryStatus? status,
        [FromQuery] string? repository,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25,
        CancellationToken cancellationToken = default)
    {
        var query = db.GitHubWebhookDeliveries.AsNoTracking();

        if (courseId is not null)
            query = query.Where(d => d.CourseId == courseId);

        if (status is not null)
            query = query.Where(d => d.Status == status);

        if (!string.IsNullOrWhiteSpace(repository))
        {
            var term = repository.Trim();
            query = query.Where(d => EF.Functions.Like(d.RepositoryFullName, $"%{term}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        // ⚠️ Written out inline rather than through a helper method. A method call in a Select cannot be
        // translated, so EF evaluates it on the client — which both loads the payload column it is the point
        // of this projection to avoid, and leaves Course unpopulated.
        var items = await query
            .OrderByDescending(d => d.Id)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, MaxPageSize))
            .Select(d => new WebhookDeliveryDto
            {
                Id = d.Id,
                CourseId = d.CourseId,
                CourseSlug = d.Course!.Slug,
                CourseName = d.Course.Name,
                DeliveryId = d.DeliveryId,
                EventName = d.EventName,
                RepositoryFullName = d.RepositoryFullName,
                ReceivedAt = d.ReceivedAt,
                CompletedAt = d.CompletedAt,
                Status = d.Status,
                AttemptCount = d.AttemptCount,
                NextAttemptAt = d.NextAttemptAt,
                HandlerCount = d.HandlerCount,
                FailedHandlerCount = d.FailedHandlerCount,
                HasPayload = d.Payload != null,
                Error = d.Error,
            })
            .ToListAsync(cancellationToken);

        return Ok(new WebhookDeliveryListDto
        {
            Items = items,
            Total = total,
            Counts = await CountsAsync(courseId, cancellationToken),
        });
    }

    /// <summary>One delivery with what each handler made of it. Still without the payload.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WebhookDeliveryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WebhookDeliveryDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        var delivery = await db.GitHubWebhookDeliveries.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (delivery is null)
            return NotFound();

        return Ok(new WebhookDeliveryDetailDto
        {
            Delivery = await ProjectOneAsync(delivery, cancellationToken),
            Outcomes = WebhookHandlerOutcome.ReadList(delivery.OutcomesJson),
        });
    }

    /// <summary>
    /// The raw payload, as text. Its own endpoint rather than a field on the detail: it is by far the largest
    /// part of the record, and it carries commit messages and author emails out of private student
    /// repositories — reading it should be a thing an administrator chose to do.
    /// </summary>
    [HttpGet("{id:int}/payload")]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayload(int id, CancellationToken cancellationToken)
    {
        var payload = await db.GitHubWebhookDeliveries.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => d.Payload)
            .FirstOrDefaultAsync(cancellationToken);

        return payload is null ? NotFound() : Content(payload, "text/plain");
    }

    /// <summary>
    /// Puts a finished delivery back on the queue.
    ///
    /// <para>By default the handlers that already succeeded are left alone — the processor derives that from
    /// the outcomes already on the row, which is why "re-run everything" is expressed here by clearing them
    /// rather than by a flag travelling down to the worker.</para>
    /// </summary>
    [HttpPost("{id:int}/retry")]
    [ProducesResponseType(typeof(WebhookDeliveryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WebhookDeliveryDto>> Retry(
        int id, [FromBody] WebhookDeliveryRetryRequest? request, CancellationToken cancellationToken)
    {
        var delivery = await db.GitHubWebhookDeliveries.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (delivery is null)
            return NotFound();

        if (delivery.Status == GitHubWebhookDeliveryStatus.Processing)
            return Conflict(new { error = "This delivery is being processed right now." });

        if (string.IsNullOrEmpty(delivery.Payload))
        {
            // Without this the re-run would dispatch an empty body and come back as "request body was empty",
            // which reads like a GitHub fault rather than our retention policy.
            return Conflict(new { error = "The payload of this delivery is no longer retained, so it cannot be re-run." });
        }

        if (request?.OnlyFailedHandlers == false)
        {
            delivery.OutcomesJson = null;
            delivery.HandlerCount = 0;
            delivery.FailedHandlerCount = 0;
        }

        delivery.Status = GitHubWebhookDeliveryStatus.Pending;
        delivery.NextAttemptAt = timeProvider.GetUtcNow();
        delivery.CompletedAt = null;
        delivery.Error = null;

        // Attempts start again: the earlier failure was investigated, and the retry budget exists to stop a
        // broken delivery looping, not to punish one an administrator deliberately resubmitted.
        delivery.AttemptCount = 0;

        await db.SaveChangesAsync(cancellationToken);

        return Ok(await ProjectOneAsync(delivery, cancellationToken));
    }

    private async Task<WebhookDeliveryDto> ProjectOneAsync(GitHubWebhookDelivery delivery, CancellationToken cancellationToken)
    {
        var course = await db.Courses.AsNoTracking()
            .Where(c => c.Id == delivery.CourseId)
            .Select(c => new { c.Slug, c.Name })
            .FirstOrDefaultAsync(cancellationToken);

        return new WebhookDeliveryDto
        {
            Id = delivery.Id,
            CourseId = delivery.CourseId,
            CourseSlug = course?.Slug ?? string.Empty,
            CourseName = course?.Name ?? string.Empty,
            DeliveryId = delivery.DeliveryId,
            EventName = delivery.EventName,
            RepositoryFullName = delivery.RepositoryFullName,
            ReceivedAt = delivery.ReceivedAt,
            CompletedAt = delivery.CompletedAt,
            Status = delivery.Status,
            AttemptCount = delivery.AttemptCount,
            NextAttemptAt = delivery.NextAttemptAt,
            HandlerCount = delivery.HandlerCount,
            FailedHandlerCount = delivery.FailedHandlerCount,
            HasPayload = delivery.Payload != null,
            Error = delivery.Error,
        };
    }

    /// <summary>
    /// Tallies over the last 24 hours — except <c>Pending</c>, which is the queue depth and so is counted
    /// whatever its age. A delivery waiting since yesterday is exactly the thing worth seeing.
    /// </summary>
    private async Task<WebhookDeliveryCountsDto> CountsAsync(int? courseId, CancellationToken cancellationToken)
    {
        var since = timeProvider.GetUtcNow().AddHours(-24);

        var scope = db.GitHubWebhookDeliveries.AsNoTracking();
        if (courseId is not null)
            scope = scope.Where(d => d.CourseId == courseId);

        var recent = scope.Where(d => d.ReceivedAt >= since || d.Status == GitHubWebhookDeliveryStatus.Pending);

        var byStatus = await recent
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(GitHubWebhookDeliveryStatus status) =>
            byStatus.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        return new WebhookDeliveryCountsDto
        {
            Pending = CountOf(GitHubWebhookDeliveryStatus.Pending) + CountOf(GitHubWebhookDeliveryStatus.Processing),
            Succeeded = CountOf(GitHubWebhookDeliveryStatus.Succeeded),
            Failed = CountOf(GitHubWebhookDeliveryStatus.Failed),
            Interrupted = CountOf(GitHubWebhookDeliveryStatus.Interrupted),
            Skipped = CountOf(GitHubWebhookDeliveryStatus.Skipped),
        };
    }
}
