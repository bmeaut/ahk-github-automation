using System;
using System.Security.Cryptography;
using System.Text;

namespace Ahk.GradeManagement.Services
{
    internal static class SecureStringGenerator
    {
        private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        public static string GetUniqueToken(int length)
        {
            byte[] randBytes = new byte[4 * length];
            using var generator = RandomNumberGenerator.Create();
            generator.GetBytes(randBytes);

            var result = new StringBuilder(capacity: length);
            for (int i = 0; i < length; i++)
            {
                var rnd = BitConverter.ToUInt32(randBytes, i * 4);
                var idx = (int)(rnd % Chars.Length);
                result.Append(Chars[idx]);
            }

            return result.ToString();
        }
    }
}
