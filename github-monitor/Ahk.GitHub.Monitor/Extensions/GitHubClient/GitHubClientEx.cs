namespace Octokit
{
    internal class GitHubClientEx : GitHubClient, IGitHubClientEx
    {
        public GitHubClientEx(ProductHeaderValue productInformation)
            : base(productInformation)
        {
            this.Actions = new ActionsClient(this);
        }

        public IActionsClient Actions { get; private set; }
    }
}
