using System.Threading.Channels;

namespace Ahk.Web.Services.Health;

/// <summary>
/// Hands course ids to <c>CourseHealthRefreshWorker</c> so a stale verdict can be refreshed off the request
/// thread. A course health run costs seconds of GitHub round-trips; no screen may wait for one.
/// </summary>
public interface ICourseHealthRefreshQueue
{
    /// <summary>Asks for a course to be re-checked in the background. Never blocks.</summary>
    void Enqueue(int courseId);

    /// <summary>Course ids to refresh, in order, until the host shuts down.</summary>
    IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Unbounded in-memory channel. Deliberately without a de-duplication set: the worker re-reads the course's
/// timestamp and skips anything already refreshed, so twenty admins opening the course register at once cost
/// one run, and the queue keeps no state that could disagree with the database.
///
/// <para>Losing the queue on restart is harmless — it holds requests to refresh a cache, and the next page
/// view queues them again.</para>
/// </summary>
public sealed class CourseHealthRefreshQueue : ICourseHealthRefreshQueue
{
    private readonly Channel<int> channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
    {
        SingleReader = true,
    });

    public void Enqueue(int courseId) => channel.Writer.TryWrite(courseId);

    public IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}
