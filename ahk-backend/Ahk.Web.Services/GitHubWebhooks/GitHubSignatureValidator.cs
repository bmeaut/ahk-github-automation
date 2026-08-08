using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Ahk.Web.Services.GitHubWebhooks;

/// <summary>
/// Validates GitHub's <c>X-Hub-Signature-256</c> header against the course's webhook secret. Ported verbatim
/// from <c>github-monitor/.../Helpers/GitHubSignatureValidator.cs</c>; the ASCII key encoding is GitHub's
/// scheme, not an oversight, and must not be "fixed" to UTF-8.
/// </summary>
public static class GitHubSignatureValidator
{
    public static bool IsSignatureValid(string requestBody, string? receivedSignature, string? secret)
    {
        if (string.IsNullOrEmpty(receivedSignature) || string.IsNullOrEmpty(secret))
            return false;

        var key = Encoding.ASCII.GetBytes(secret);
        var requestBytes = Encoding.UTF8.GetBytes(requestBody ?? string.Empty);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(requestBytes);
        var expectedSignature = "sha256=" + ToHexString(hash);

        // Compare length first, do not even try to compare content if these do not match.
        if (receivedSignature.Length != expectedSignature.Length)
            return false;

        return receivedSignature.Equals(expectedSignature, StringComparison.Ordinal);
    }

    private static string ToHexString(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            builder.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);

        return builder.ToString();
    }
}
