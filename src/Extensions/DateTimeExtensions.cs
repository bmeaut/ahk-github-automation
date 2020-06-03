using System;

namespace Ahk.GitHub.Monitor.Extensions
{
    static class DateTimeExtensions
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static int ToUnixTimeStamp(this DateTime date)
        {
            return (int)(date - UnixEpoch).TotalSeconds;
        }
    }
}
