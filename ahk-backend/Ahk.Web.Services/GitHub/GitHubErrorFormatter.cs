using System.Globalization;
using System.Text;
using Octokit;

namespace Ahk.Web.Services.GitHub;

/// <summary>
/// Turns an exception from a GitHub call into something an administrator can act on.
///
/// <para>It exists because <see cref="ApiException"/> hides its own diagnosis. Octokit's
/// <c>ApiException.Message</c> is <c>ApiErrorMessageSafe ?? "An error occurred with this API request"</c>, so
/// whenever GitHub answers without a parseable <c>message</c> field — which is exactly what a gateway 5xx
/// does — the exception says nothing at all, and recording <c>ex.ToString()</c> preserved only that nothing.
/// The status code, GitHub's own error detail and the request id are all on the exception; this reads them
/// off. A production failure had to be diagnosed by reasoning about which exception type Octokit had
/// <em>not</em> thrown, which is the situation this removes.</para>
///
/// <para>⚠️ Only the response is ever read. The request carries the installation token and must never reach a
/// log or a delivery row.</para>
/// </summary>
public static class GitHubErrorFormatter
{
    /// <summary>Longest slice of GitHub's raw body kept. Enough to recognise an HTML error page.</summary>
    private const int MaxBodyLength = 500;

    private const string RequestIdHeader = "x-github-request-id";

    /// <summary>
    /// One line naming what GitHub returned. Used for <c>GitHubWebhookDelivery.Error</c>, which the admin
    /// console shows as the delivery's headline.
    /// </summary>
    public static string Describe(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        try
        {
            return exception switch
            {
                ApiException api => DescribeApiException(api),

                // Octokit takes no cancellation token on these calls, so a cancellation here is always our own
                // request timeout elapsing. Say so, or it reads as GitHub having rejected something.
                TaskCanceledException or TimeoutException =>
                    $"{exception.GetType().Name}: the portal's own GitHub request timeout elapsed before GitHub answered. {exception.Message}",

                _ => $"{exception.GetType().Name}: {exception.Message}",
            };
        }
#pragma warning disable CA1031 // This runs on the error path; a second failure here would hide the first.
        catch (Exception failure)
#pragma warning restore CA1031
        {
            return $"{exception.GetType().Name}: {exception.Message} (the error could not be described in full: {failure.Message})";
        }
    }

    /// <summary>
    /// <see cref="Describe"/> plus the full exception, for <c>WebhookHandlerOutcome.Error</c> — the stack is
    /// what says which handler step failed, and the first line is what says why.
    /// </summary>
    public static string Summarize(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return $"{Describe(exception)}{Environment.NewLine}{exception}";
    }

    private static string DescribeApiException(ApiException exception)
    {
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"{exception.GetType().Name}: GitHub returned {(int)exception.StatusCode} {exception.StatusCode}");

        // Flattened, because this is not always GitHub's own one-line message: when the body does not parse as
        // JSON, Octokit puts the whole raw body here — an HTML error page, newlines and all.
        var gitHubMessage = string.IsNullOrWhiteSpace(exception.ApiError?.Message)
            ? null
            : Flatten(exception.ApiError!.Message);

        text.Append("; message: ").Append(gitHubMessage ?? "(no message in response body)");

        var errors = exception.ApiError?.Errors;
        if (errors is { Count: > 0 })
            text.Append("; errors: ").Append(string.Join(", ", errors.Select(DescribeErrorDetail)));

        // The one value GitHub support asks for first.
        var requestId = ReadHeader(exception.HttpResponse, RequestIdHeader);
        if (!string.IsNullOrEmpty(requestId))
            text.Append("; request-id: ").Append(requestId);

        // Skipped when it is the message already printed, which is the case for every body Octokit could not
        // parse as JSON.
        var body = ReadBody(exception.HttpResponse);
        if (!string.IsNullOrEmpty(body) && !string.Equals(body, gitHubMessage, StringComparison.Ordinal))
            text.Append("; body: ").Append(body);

        return text.ToString();
    }

    private static string DescribeErrorDetail(ApiErrorDetail detail)
    {
        if (detail is null)
            return "(null)";

        var parts = new[] { detail.Resource, detail.Field, detail.Code, detail.Message }
            .Where(p => !string.IsNullOrWhiteSpace(p));

        var text = string.Join("/", parts);
        return string.IsNullOrEmpty(text) ? "(empty)" : text;
    }

    /// <summary>
    /// ⚠️ <see cref="ApiException.HttpResponse"/> is null for the exceptions built from the
    /// <c>(string, HttpStatusCode)</c> constructor, and Octokit's header dictionary makes no promise about
    /// casing — hence the explicit lookup rather than an indexer.
    /// </summary>
    private static string? ReadHeader(IResponse? response, string name)
    {
        if (response?.Headers is null)
            return null;

        foreach (var header in response.Headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
                return header.Value;
        }

        return null;
    }

    /// <summary>
    /// GitHub's raw body, flattened to one line and truncated. <see cref="IResponse.Body"/> is an
    /// <see cref="object"/>: Octokit hands back a string for text content types and a byte array otherwise, and
    /// the byte-array case is itself worth reporting — it is why <c>ApiError.Message</c> came back empty.
    /// </summary>
    private static string? ReadBody(IResponse? response)
    {
        switch (response?.Body)
        {
            case null:
                return null;

            case string text when !string.IsNullOrWhiteSpace(text):
                return Flatten(text);

            case string:
                return null;

            case byte[] bytes:
                return $"({bytes.Length} bytes of {response.ContentType ?? "an unnamed content type"})";

            default:
                return null;
        }
    }

    private static string Flatten(string text)
    {
        var flattened = new StringBuilder(Math.Min(text.Length, MaxBodyLength));
        var lastWasSpace = false;

        foreach (var c in text.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace && flattened.Length > 0)
                    flattened.Append(' ');

                lastWasSpace = true;
            }
            else
            {
                flattened.Append(c);
                lastWasSpace = false;
            }

            if (flattened.Length >= MaxBodyLength)
                return flattened.Append('…').ToString();
        }

        return flattened.ToString();
    }
}
