using System;
using System.Security.Cryptography;
using System.Text;

namespace Ahk.GitHub.Monitor
{
    public static class PayloadValidator
    {
        public static bool IsSignatureValid(byte[] payloadBytes, string receivedSignature, string secret)
        {
            if (string.IsNullOrEmpty(receivedSignature))
                return false;

            var key = Encoding.ASCII.GetBytes(secret);
            var hash = new HMACSHA1(key).ComputeHash(payloadBytes);
            var expectedSignature = "sha1=" + hash.toHexString();

            // compare length first, do not even try to compare content if these do not match
            if (receivedSignature.Length != expectedSignature.Length)
                return false;

            return receivedSignature.Equals(expectedSignature, StringComparison.Ordinal);
        }

        private static string toHexString(this byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                builder.AppendFormat("{0:x2}", b);
            return builder.ToString();
        }
    }
}
