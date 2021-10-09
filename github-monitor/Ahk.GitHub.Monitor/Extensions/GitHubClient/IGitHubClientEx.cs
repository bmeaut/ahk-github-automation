namespace Octokit
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Ex suffix considered and used.")]
    public interface IGitHubClientEx : IGitHubClient
    {
        IActionsClient Actions { get; }
    }
}
