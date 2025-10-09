using System;
using System.Security.Cryptography;
using System.Text;

namespace Ahk.GitHub.Monitor.Helpers;

public static class GitHubSignatureValidator
{
    public static bool IsSignatureValid(string requestBody, string receivedSignature, string secret)
    {
        if (string.IsNullOrEmpty(receivedSignature))
        {
            return false;
        }

        var key = Encoding.ASCII.GetBytes(secret);
        var requestBytes = Encoding.UTF8.GetBytes(requestBody);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(requestBytes);
        var expectedSignature = "sha256=" + hash.ToHexString();

        // compare length first, do not even try to compare content if these do not match
        if (receivedSignature.Length != expectedSignature.Length)
        {
            return false;
        }

        return receivedSignature.Equals(expectedSignature, StringComparison.Ordinal);
    }

    private static string ToHexString(this byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            builder.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:x2}", b);
        }

        return builder.ToString();
    }
}
