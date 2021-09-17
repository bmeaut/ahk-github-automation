package appargs

import (
	"encoding/json"
	"errors"
	"fmt"
	"io/ioutil"
	"os"
	"strings"
)

type argsFromGitHubAction struct {
}

func NewGitHubActionArgsReader() ArgsReader {
	return new(argsFromGitHubAction)
}

func (r argsFromGitHubAction) GetArgs() (args *AppArgs, err error) {
	// getting most information is only possible when running inside GitHub Action.
	// Assert that the environment is such.
	_, err = getRequiredEnv("GITHUB_ACTIONS")
	if err != nil {
		return nil, errors.New("GITHUB_ACTIONS not set (not running inside GitHub Actions)")
	}

	// These variables are implicitly set bu GitHub Action for the action execution.
	ghRepoFullName, err := getRequiredEnv("GITHUB_REPOSITORY")
	if err != nil {
		return nil, err
	}
	ghRepoSplit := strings.Split(ghRepoFullName, "/")
	if len(ghRepoSplit) != 2 {
		return nil, errors.New("GITHUB_REPOSITORY is not in expected format owner/name")
	}
	ghRepoOwner := ghRepoSplit[0]
	ghRepoName := ghRepoSplit[1]

	ghBranch, err := getRequiredEnv("GITHUB_REF")
	if err != nil {
		return nil, err
	}

	ghCommitHash, err := getRequiredEnv("GITHUB_SHA")
	if err != nil {
		return nil, err
	}

	ghActionRunId := getEnvOrDefault("GITHUB_RUN_ID", "")

	// The following are app settings passed using the "with" directive
	// the name of the environment variable is prefixed with INPUT_
	neptunFileName := getEnvOrDefault("INPUT_AHK_NEPTUNFILENAME", "neptun.txt")
	resultsFile := getEnvOrDefault("INPUT_AHK_RESULTFILE", "result.txt")
	imageExt := getEnvOrDefault("INPUT_AHK_IMAGEEXT", "")

	ahkAppUrl := getEnvOrDefault("INPUT_AHK_APPURL", "https://ahk-grade-management.azurewebsites.net/api/evaluation-result")
	ahkAppToken := getEnvOrDefault("INPUT_AHK_APPTOKEN", "")
	ahkAppSecret := getEnvOrDefault("INPUT_AHK_APPSECRET", "")

	// The following is the token to communicate with GitHub. Passed using the "with" directive too.
	ghToken, err := getRequiredEnv("INPUT_GITHUB_TOKEN")
	if err != nil {
		return nil, err
	}

	// The action expects to be executed within a pull request context. This information is
	// parsed from the pull request event payload event present as a file.
	ghEventPayloadFilePath, err := getRequiredEnv("GITHUB_EVENT_PATH")
	if err != nil {
		return nil, err
	}

	ghPrNum, err := getPrNumFromPayload(ghEventPayloadFilePath)
	if err != nil {
		return nil, err
	}

	return &AppArgs{
		GitHubRepoFullName:   ghRepoFullName,
		GitHubRepoOwner:      ghRepoOwner,
		GitHubRepoName:       ghRepoName,
		GitHubBranch:         ghBranch,
		GitHubActionRunId:    ghActionRunId,
		GitCommitHash:        ghCommitHash,
		GitHubPullRequestNum: ghPrNum,
		GitHubToken:          ghToken,
		NeptunFileName:       neptunFileName,
		ImageExtension:       imageExt,
		ResultFile:           resultsFile,
		AhkAppUrl:            ahkAppUrl,
		AhkAppToken:          ahkAppToken,
		AhkAppSecret:         ahkAppSecret,
	}, nil
}

func getRequiredEnv(key string) (value string, err error) {
	value, success := os.LookupEnv(key)
	if !success || value == "" {
		return "", fmt.Errorf("missing required environment variable %s", key)
	}
	return value, nil
}

func getEnvOrDefault(key string, defaultValue string) string {
	value, success := os.LookupEnv(key)
	if !success {
		return defaultValue
	}
	return value
}

type GitHubEventPayload struct {
	PullRequest struct {
		Number int `json:"number"`
	} `json:"pull_request"`
}

func getPrNumFromPayload(ghEventPayloadFile string) (value int, err error) {
	f, err := os.Open(ghEventPayloadFile)
	if err != nil {
		return -1, fmt.Errorf("file at %s (from GITHUB_EVENT_PATH) does not exist", ghEventPayloadFile)
	}
	defer f.Close()

	rawBytes, err := ioutil.ReadAll(f)
	if err != nil {
		return -1, fmt.Errorf("file at %s (from GITHUB_EVENT_PATH) cannot be read", ghEventPayloadFile)
	}

	payload := GitHubEventPayload{}
	err = json.Unmarshal(rawBytes, &payload)
	if err != nil {
		return -1, errors.New("not running within a pull request event context")
	}

	return payload.PullRequest.Number, nil
}
