package publishtopr

type Config struct {
	GitHubToken string
	RepoOwner   string
	RepoName    string
	PrNumber    int
	CommitHash  string
}
