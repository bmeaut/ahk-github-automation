using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Ahk.Web.Services.Integrations;

/// <summary>
/// Verifies the signature the evaluator container puts on an evaluation result. Ported verbatim from
/// <c>grade-management/.../Helpers/HmacSha256Validator.cs</c>; the counterpart that produces these signatures
/// is Go, in <c>publish-results-pr/internal/publishtoapi</c>, and the two are pinned to each other by an
/// identical set of golden vectors in both test suites.
///
/// <para>The string to sign is four parts joined by single <c>\n</c> characters, with no trailing newline:
/// <c>UPPERCASE(verb)</c>, <c>lowercase(url)</c>, the RFC1123 date, then the raw body. Two details are
/// load-bearing and must not be "modernised": the key is <strong>ASCII</strong> bytes (matching Go's
/// <c>[]byte(secret)</c> for the ASCII-only secrets the generator produces), and the <em>whole URL including
/// any query string</em> is signed — which is why the callback URL has to match byte for byte on both
/// sides.</para>
/// </summary>
public static class HmacSha256Validator
{
    public static bool IsSignatureValid(string httpVerb, string httpUrl, DateTime date, string requestBody, string? receivedSignature, string? secret)
    {
        if (string.IsNullOrEmpty(receivedSignature) || string.IsNullOrEmpty(secret))
            return false;

        var key = Encoding.ASCII.GetBytes(secret);
        var payloadSignedBytes = GetBytesToSign(httpVerb, httpUrl, date, requestBody);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(payloadSignedBytes);
        var expectedSignature = Convert.ToBase64String(hash);

        // Constant-time: grade-management used string.Equals here, which returns at the first differing
        // character and lets a forged signature be refined one character at a time by timing. See
        // SignatureComparison.
        return SignatureComparison.FixedTimeEquals(receivedSignature, expectedSignature);
    }

    /// <summary>The signed string, exposed so a failing request can be diagnosed without leaking the secret.</summary>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "URL normalized to lowercase by design; the Go client does the same.")]
    public static string GetStringToSign(string httpVerb, string httpUrl, DateTime date, string requestBody)
        => string.Concat(
            httpVerb.ToUpperInvariant(),
            "\n",
            httpUrl.ToLowerInvariant(),
            "\n",
            date.ToString("R", CultureInfo.InvariantCulture),
            "\n",
            requestBody);

    private static byte[] GetBytesToSign(string httpVerb, string httpUrl, DateTime date, string requestBody)
        => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(GetStringToSign(httpVerb, httpUrl, date, requestBody));
}
