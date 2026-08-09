using System.Text;

namespace Ahk.Web.Server.Integrations;

/// <summary>
/// Reads a request body as the exact string that was signed.
///
/// Both machine-to-machine schemes here (GitHub's <c>X-Hub-Signature-256</c> and the CI callback's
/// <c>X-Ahk-Sha256</c>) compute their HMAC over the raw bytes, so the body must not be round-tripped through
/// model binding first. The controllers therefore declare no <c>[FromBody]</c> parameter, MVC never touches the
/// stream, and this reads it once — no <c>EnableBuffering</c> anywhere in the pipeline.
/// </summary>
internal static class RawBody
{
    public static async Task<string> ReadAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
