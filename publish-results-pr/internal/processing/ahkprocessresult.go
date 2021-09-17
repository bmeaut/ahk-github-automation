package processing

import resultsfileparser "ahk/publishresultpr/internal/processing/resultsfile"

type AhkProcessResult struct {
	GitHubRepoName       string                            `json:"gitHubRepoName"`
	GitHubBranch         string                            `json:"gitHubBranch"`
	GitHubPullRequestNum int                               `json:"gitHubPullRequestNum,omitempty"`
	GitHubCommitHash     string                            `json:"gitHubCommitHash"`
	NeptunCode           string                            `json:"neptunCode"`
	ImageFiles           []string                          `json:"imageFiles"`
	Result               []resultsfileparser.AhkTaskResult `json:"result"`
	Origin               string                            `json:"origin,omitempty"`
}
