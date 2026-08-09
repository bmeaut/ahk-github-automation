using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Ahk.Web.Server.CourseContext;
using Ahk.Web.Server.Integrations.Dto;
using Ahk.Web.Services.Courses;
using Ahk.Web.Services.Grading;
using Ahk.Web.Services.Grading.Dto;
using Ahk.Web.Services.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Ahk.Web.Server.Integrations;

/// <summary>
/// Receives an automated evaluation result from the GitHub Action that runs in a student's repository —
/// the port of grade-management's <c>evaluation-result</c> function.
///
/// <para>⚠️ The caller signs the request <em>URL</em>, so this route's public address is part of the contract:
/// <c>https://ahk.aut.bme.hu/api/integrations/evaluation-result</c>, https, no trailing slash. It must match the
/// action's <c>AHK_APPURL</c> exactly (case aside, which both sides lower). <c>UseForwardedHeaders</c> running
/// first in the pipeline is what makes <see cref="UriHelper.GetDisplayUrl"/> yield the public URL behind IIS.
/// See <c>docs/ci-callback.md</c>.</para>
///
/// <para>The Go client treats any non-2xx as fatal and fails the student's build, so the failure modes here are
/// deliberately limited to genuine misconfiguration.</para>
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/integrations/evaluation-result")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class EvaluationResultController : ControllerBase
{
    /// <summary>How far the caller's clock may drift. Carried over unchanged; GitHub-hosted runners are UTC.</summary>
    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions PayloadOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IWebhookTokenService tokens;
    private readonly ICourseResolutionService courses;
    private readonly IGradeService grades;
    private readonly CurrentCourseProvider currentCourse;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<EvaluationResultController> logger;

    public EvaluationResultController(
        IWebhookTokenService tokens,
        ICourseResolutionService courses,
        IGradeService grades,
        CurrentCourseProvider currentCourse,
        TimeProvider timeProvider,
        ILogger<EvaluationResultController> logger)
    {
        this.tokens = tokens;
        this.courses = courses;
        this.grades = grades;
        this.currentCourse = currentCourse;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        var token = Request.Headers["X-Ahk-Token"].FirstOrDefault();
        var receivedSignature = Request.Headers["X-Ahk-Sha256"].FirstOrDefault();
        var deliveryId = Request.Headers["X-Ahk-Delivery"].FirstOrDefault();
        var dateStr = Request.Headers[HeaderNames.Date].FirstOrDefault();

        logger.LogInformation(
            "evaluation-result request with X-Ahk-Delivery='{DeliveryId}', X-Ahk-Token = '{Token}'", deliveryId, MaskToken(token));

        // Order and wording of these checks are the ported contract; the evaluator's logs are full of them.
        if (string.IsNullOrEmpty(dateStr))
            return BadRequest(new { error = "Date header missing" });

        if (!DateTime.TryParseExact(
                dateStr,
                "R",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var date))
        {
            return BadRequest(new { error = "Date header value not valid RFC1123 string" });
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (date < now.Add(-MaxClockSkew) || date > now.Add(MaxClockSkew))
            return BadRequest(new { error = "Date header value is not close enough to current date" });

        if (string.IsNullOrEmpty(receivedSignature))
            return BadRequest(new { error = "X-Ahk-Sha256 header missing" });

        if (string.IsNullOrEmpty(token))
            return BadRequest(new { error = "X-Ahk-Token header missing" });

        var secret = await tokens.GetSecretForTokenAsync(token, cancellationToken);
        if (string.IsNullOrEmpty(secret))
            return BadRequest(new { error = "X-Ahk-Token invalid" });

        var requestBody = await RawBody.ReadAsync(Request, cancellationToken);
        var signedUrl = Request.GetDisplayUrl();

        if (!HmacSha256Validator.IsSignatureValid(Request.Method, signedUrl, date, requestBody, receivedSignature, secret))
        {
            // The URL is the usual culprit and the only part of the signed string that is safe to log — never
            // the body (student code) and never the secret.
            logger.LogDebug(
                "evaluation-result signature mismatch for X-Ahk-Delivery='{DeliveryId}'; signed URL was '{Url}'", deliveryId, signedUrl);
            return BadRequest(new { error = "X-Ahk-Sha256 signature not valid" });
        }

        if (!TryReadPayload(requestBody, out var payload, out var deserializationError))
            return BadRequest(new { error = deserializationError });

        // New in the portal: one deployment serves every course, so the course comes from the token — the
        // authenticated credential — rather than from the caller-supplied repository name.
        var course = await courses.ResolveByWebhookTokenAsync(token, cancellationToken);
        if (course is null)
        {
            // Same message as an unknown token: a caller must not be able to tell a revoked token from one
            // whose course was deleted.
            logger.LogWarning("evaluation-result token '{Token}' resolved to no course", MaskToken(token));
            return BadRequest(new { error = "X-Ahk-Token invalid" });
        }

        var repositoryCourse = await courses.ResolveByRepositoryAsync(payload.GitHubRepoName, cancellationToken);
        if (repositoryCourse is not null && repositoryCourse.Id != course.Id)
        {
            // Not fatal — the token is authoritative — but it means a course is using another course's token,
            // and the grade is about to land in the wrong place.
            logger.LogWarning(
                "evaluation-result for '{Repository}' resolves to course '{RepositoryCourse}' but its token belongs to '{TokenCourse}'",
                payload.GitHubRepoName, repositoryCourse.Slug, course.Slug);
        }

        currentCourse.Set(course.Id);

        logger.LogInformation(
            "evaluation-result request with X-Ahk-Delivery='{DeliveryId}' accepted, starting processing", deliveryId);

        try
        {
            await grades.RecordEvaluationResultAsync(course.Id, ToInput(payload), date, cancellationToken);
            logger.LogInformation("evaluation-result request handled with success for X-Ahk-Delivery='{DeliveryId}'", deliveryId);
            return Ok();
        }
#pragma warning disable CA1031 // Ported shape: the caller gets the failure text, because it is a build log.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "evaluation-result webhook failed for X-Ahk-Delivery='{DeliveryId}'", deliveryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.ToString() });
        }
    }

    /// <summary>
    /// A token is a live credential: it selects a course and is half of the pair that authenticates a grade
    /// write. grade-management logged it whole, which put working credentials into any log the application
    /// ships to. Only enough is kept to tell two tokens apart while diagnosing — the same last-four convention
    /// the admin API uses when it reports a stored secret.
    /// </summary>
    private static string MaskToken(string? token)
        => string.IsNullOrEmpty(token) ? "(none)" : $"…{token[^Math.Min(4, token.Length)..]}";

    private static EvaluationResultInput ToInput(EvaluationResultRequest payload)
        => new()
        {
            NeptunCode = payload.NeptunCode,
            GitHubRepoName = payload.GitHubRepoName,
            GitHubBranch = payload.GitHubBranch,
            GitHubCommitHash = payload.GitHubCommitHash,
            GitHubPullRequestNum = payload.GitHubPullRequestNum,
            Origin = payload.Origin,
            Result = payload.Result?.Select(r => new EvaluationTaskResult
            {
                ExerciseName = r.ExerciseName,
                TaskName = r.TaskName,
                Points = r.Points,
                Comment = r.Comment,
            }).ToList() ?? new List<EvaluationTaskResult>(),
        };

    /// <summary>Port of grade-management's <c>PayloadReader</c>, error string included.</summary>
    private static bool TryReadPayload(string requestBody, out EvaluationResultRequest payload, out string error)
    {
        payload = null!;
        error = null!;

        try
        {
            var result = JsonSerializer.Deserialize<EvaluationResultRequest>(requestBody, PayloadOptions);
            if (result is null)
            {
                error = "Body cannot be deserialized as JSON: the body was null";
                return false;
            }

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(result, new ValidationContext(result, null, null), validationResults, validateAllProperties: true))
            {
                error = $"Body cannot be deserialized as JSON: {string.Join(", ", validationResults.Select(s => s.ErrorMessage).ToArray())}";
                return false;
            }

            payload = result;
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Body cannot be deserialized as JSON: {ex.Message}";
            return false;
        }
    }
}
