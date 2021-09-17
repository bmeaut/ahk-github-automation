using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ahk.GradeManagement
{
    public static class HmacSha256Validator
    {
        public static bool IsSignatureValid(string httpVerb, string httpUrl, DateTime date, string requestBody, string receivedSignature, string secret)
        {
            if (string.IsNullOrEmpty(receivedSignature))
                return false;

            var key = Encoding.ASCII.GetBytes(secret);
            var payloadSignedBytes = getBytesToSign(httpVerb, httpUrl, date, requestBody);
            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(payloadSignedBytes);
            var expectedSignature = Convert.ToBase64String(hash);

            // compare length first, do not even try to compare content if these do not match
            if (receivedSignature.Length != expectedSignature.Length)
                return false;

            return receivedSignature.Equals(expectedSignature, StringComparison.Ordinal);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "URL normalized to lowercase bz design.")]
        private static byte[] getBytesToSign(string httpVerb, string httpUrl, DateTime date, string requestBody)
        {
            using var streamToHash = new MemoryStream();
            using var contentToHash = new StreamWriter(streamToHash);

            contentToHash.Write(httpVerb.ToUpperInvariant() + '\n');
            contentToHash.Write(httpUrl.ToLowerInvariant() + '\n');
            contentToHash.Write(date.ToString("R") + '\n');
            contentToHash.Write(requestBody);

            contentToHash.Flush();
            return streamToHash.ToArray();
        }
    }
}
