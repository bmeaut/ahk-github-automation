package appargs

type AppArgs struct {
	GitHubRepoFullName   string
	GitHubRepoOwner      string
	GitHubRepoName       string
	GitHubBranch         string
	GitCommitHash        string
	GitHubActionRunId    string
	GitHubPullRequestNum int
	GitHubToken          string
	NeptunFileName       string
	ImageExtension       string
	ResultFile           string
	AhkAppUrl            string
	AhkAppSecret         string
	AhkAppToken          string
}

type ArgsReader interface {
	GetArgs() (args *AppArgs, err error)
}
