namespace Ahk.GitHub.Monitor
{
    public class RepositorySettings
    {
        public bool Enabled { get; set; } = false;
        public BranchProtectionSettings BranchProtection { get; set; } = new BranchProtectionSettings();
        public CommentProtectionSettings CommentProtection { get; set; } = new CommentProtectionSettings();
        public MultiplePRProtectionSettings MultiplePRProtection { get; set; } = new MultiplePRProtectionSettings();
    }
}
