using Microsoft.Data.SqlClient;

using System.Net.Sockets;

namespace Ahk.GradeManagement.Api.ErrorHandling;

public static class ExceptionExtensions
{
    private static readonly HashSet<int> RetriableSqlErrorClasses = [11, 13, 16, 17, 18, 19, 20, 21, 22, 24];
    private static readonly HashSet<int> RetriableSqlErrorNumbers = [-2, 1205];

    /// <summary>
    /// https://stackoverflow.com/a/24041546/1406798
    /// </summary>
    /// <returns>Returns true if the exception is considered as a retryable timeout</returns>
    public static bool IsDbTimeout(this Exception e) =>
        e is SqlException sqle
        && (RetriableSqlErrorClasses.Contains(sqle.Class) || RetriableSqlErrorNumbers.Contains(sqle.Number));

    public static bool IsHttpTimeout(this Exception ex) =>
        ex.InnerException is SocketException socketException && socketException.SocketErrorCode == SocketError.TimedOut;
}
