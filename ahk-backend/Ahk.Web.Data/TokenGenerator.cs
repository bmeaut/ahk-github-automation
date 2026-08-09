using System.Security.Cryptography;

namespace Ahk.Web.Data;

/// <summary>
/// Random identifiers that travel in URLs and HTTP headers: CI callback tokens and assignment invite links.
/// Base64 with the two URL-hostile characters swapped and the padding dropped, so the value can be pasted
/// anywhere without escaping.
/// </summary>
public static class TokenGenerator
{
    public static string UrlSafe(int byteLength) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
