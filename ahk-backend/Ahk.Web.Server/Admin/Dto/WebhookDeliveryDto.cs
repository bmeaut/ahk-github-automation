using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHubWebhooks;

namespace Ahk.Web.Server.Admin.Dto;

/// <summary>
/// One row of the delivery list. Deliberately without the payload: it is the bulk of the record, it holds
/// commit messages and author emails from private student repositories, and reading it is a separate act
/// served by its own endpoint.
/// </summary>
public sealed class WebhookDeliveryDto
{
    public int Id { get; init; }

    public int CourseId { get; init; }

    public string CourseSlug { get; init; } = string.Empty;

    public string CourseName { get; init; } = string.Empty;

    /// <summary>The <c>X-GitHub-Delivery</c> header, the id an administrator can copy out of GitHub's own log.</summary>
    public string? DeliveryId { get; init; }

    public string EventName { get; init; } = string.Empty;

    public string RepositoryFullName { get; init; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public GitHubWebhookDeliveryStatus Status { get; init; }

    public int AttemptCount { get; init; }

    public DateTimeOffset? NextAttemptAt { get; init; }

    public int HandlerCount { get; init; }

    public int FailedHandlerCount { get; init; }

    /// <summary>Whether the raw payload is still retained, and so whether a re-run is possible.</summary>
    public bool HasPayload { get; init; }

    public string? Error { get; init; }
}

/// <summary>A delivery with what each handler made of it.</summary>
public sealed class WebhookDeliveryDetailDto
{
    public WebhookDeliveryDto Delivery { get; init; } = new();

    public IReadOnlyList<WebhookHandlerOutcome> Outcomes { get; init; } = Array.Empty<WebhookHandlerOutcome>();
}

/// <summary>A page of deliveries plus the tallies the summary tiles show, in one request.</summary>
public sealed class WebhookDeliveryListDto
{
    public IReadOnlyList<WebhookDeliveryDto> Items { get; init; } = Array.Empty<WebhookDeliveryDto>();

    public int Total { get; init; }

    public WebhookDeliveryCountsDto Counts { get; init; } = new();
}

/// <summary>Tallies over the last 24 hours, plus the queue depth, which is not time-bounded.</summary>
public sealed class WebhookDeliveryCountsDto
{
    public int Pending { get; init; }

    public int Succeeded { get; init; }

    public int Failed { get; init; }

    public int Interrupted { get; init; }

    public int Skipped { get; init; }
}

/// <summary>Body of the re-run request.</summary>
public sealed class WebhookDeliveryRetryRequest
{
    /// <summary>
    /// When true (the default) the handlers that already succeeded are skipped. Turning it off re-runs
    /// everything, which will post duplicate comments — it exists for the case where a handler succeeded
    /// against the wrong state, and it is the administrator's decision.
    /// </summary>
    public bool OnlyFailedHandlers { get; init; } = true;
}
