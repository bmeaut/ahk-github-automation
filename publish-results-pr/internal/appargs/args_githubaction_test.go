package appargs

import (
	"os"
	"path"
	"reflect"
	"strings"
	"testing"
)

func TestGitHubActionArgs_ErrorWhenMissingEnv(t *testing.T) {
	if os.Getenv("GITHUB_ACTIONS") != "" {
		t.Skip("Skipping test in GitHub Actions")
		return
	}

	envs := map[string]string{
		"GITHUB_ACTIONS":     "true",
		"GITHUB_REPOSITORY":  "org/repo",
		"GITHUB_REF":         "main",
		"GITHUB_SHA":         "aa11bbcc33",
		"GITHUB_EVENT_PATH":  "path/to.json",
		"INPUT_GITHUB_TOKEN": "ghtokghtok",
	}

	for envToIgnore := range envs {
		t.Run(envToIgnore, func(t *testing.T) {
			for key, value := range envs {
				if key != envToIgnore {
					t.Setenv(key, value)
				}
			}

			r := NewGitHubActionArgsReader()
			_, err := r.GetArgs()

			if err == nil || !strings.Contains(err.Error(), envToIgnore) {
				t.Errorf("argsFromGitHubAction should have failed with missing env variable %v", envToIgnore)
			}
		})
	}
}

func TestGitHubActionArgs_ArgsReadFromEnv(t *testing.T) {
	if os.Getenv("GITHUB_ACTIONS") != "" {
		t.Skip("Skipping test in GitHub Actions")
		return
	}

	tempDir := t.TempDir()
	tempJsonFile := path.Join(tempDir, "ghe.json")
	t.Setenv("GITHUB_EVENT_PATH", tempJsonFile)

	os.WriteFile(tempJsonFile, []byte(`{"pull_request":{"number":123}}`), 0666)

	envs := map[string]string{
		"GITHUB_ACTIONS":      "true",
		"GITHUB_REPOSITORY":   "org/repo",
		"GITHUB_REF":          "main",
		"GITHUB_SHA":          "aa11bbcc33",
		"GITHUB_RUN_ID":       "2112121212",
		"INPUT_GITHUB_TOKEN":  "ghtokghtok",
		"INPUT_AHK_APPTOKEN":  "tokentoken",
		"INPUT_AHK_IMAGEEXT":  ".ext",
		"INPUT_AHK_APPSECRET": "secretsecret",
	}

	expected := AppArgs{
		GitHubRepoFullName:   "org/repo",
		GitHubRepoOwner:      "org",
		GitHubRepoName:       "repo",
		GitHubBranch:         "main",
		GitCommitHash:        "aa11bbcc33",
		GitHubActionRunId:    "2112121212",
		GitHubPullRequestNum: 123,
		GitHubToken:          "ghtokghtok",
		NeptunFileName:       "neptun.txt",
		ImageExtension:       ".ext",
		ResultFile:           "result.txt",
		AhkAppUrl:            `https://ahk-grade-management.azurewebsites.net/api/evaluation-result`,
		AhkAppToken:          "tokentoken",
		AhkAppSecret:         "secretsecret",
	}

	for key, value := range envs {
		t.Setenv(key, value)
	}

	r := NewGitHubActionArgsReader()
	actual, err := r.GetArgs()

	if err != nil {
		t.Errorf("argsFromGitHubAction failed with %v", err)
		return
	}

	if !reflect.DeepEqual(*actual, expected) {
		t.Errorf("argsFromGitHubAction expected %+v got %+v", expected, *actual)
	}
}
