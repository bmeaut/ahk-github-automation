using System;

namespace Ahk.GitHub.Monitor.Extensions;

internal static class DateTimeExtensions
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static int ToUnixTimeStamp(this DateTime date) => (int)(date - UnixEpoch).TotalSeconds;
}
