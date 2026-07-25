using System.Text.Json;

namespace Ahk.Web.Services.GitHub;

/// <summary>Shared constants and helpers for the raw <c>api.github.com</c> calls the portal makes.</summary>
public static class GitHubApiDefaults
{
    /// <summary>Named <see cref="HttpClient"/> registered by <c>AddAhkServices</c>, based at api.github.com.</summary>
    public const string HttpClientName = "github";

    /// <summary>
    /// GitHub's own explanation of a failure, from the <c>message</c> field of its error body. Returns null
    /// rather than throwing on an unreadable body — this runs on the error path, where a second failure would
    /// only hide the first.
    /// </summary>
    public static async Task<string?> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return document.RootElement.TryGetProperty("message", out var message) ? message.GetString() : null;
        }
        catch (Exception ex) when (ex is JsonException or HttpRequestException or InvalidOperationException)
        {
            return null;
        }
    }
}
