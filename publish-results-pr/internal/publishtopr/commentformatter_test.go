package publishtopr

import (
	resultsfileparser "ahk/publishresultpr/internal/processing/resultsfile"
	"math"
	"strings"
	"testing"
)

func TestGitHubPublisherTaskNameFormatter(t *testing.T) {
	tests := []struct {
		taskName     string
		exerciseName string
		wantValue    string
	}{
		{
			taskName:     "task",
			exerciseName: "ex",
			wantValue:    "ex / task",
		},
		{
			taskName:     "",
			exerciseName: "ex",
			wantValue:    "ex / N/A",
		},
		{
			taskName:     "task",
			exerciseName: "",
			wantValue:    "task",
		},
	}
	for _, tt := range tests {
		t.Run("TestGitHubPusherTaskNameFormatter", func(t *testing.T) {
			input := resultsfileparser.AhkTaskResult{
				ExerciseName: tt.exerciseName,
				TaskName:     tt.taskName,
			}
			gotValue := formatTaskName(input)

			if gotValue != tt.wantValue {
				t.Errorf("formatTaskName() = %v, want %v", gotValue, tt.wantValue)
			}
		})
	}
}

func TestGitHubPublisherNeptunFormatter(t *testing.T) {
	tests := []struct {
		neptun    string
		wantValue string
	}{
		{
			neptun:    "ALMA12",
			wantValue: "**Neptun**: ALMA12",
		},
		{
			neptun:    "",
			wantValue: "**Neptun**: :exclamation: N/A",
		},
	}
	for _, tt := range tests {
		t.Run("TestGitHubPublisherNeptunFormatter", func(t *testing.T) {
			str := strings.Builder{}
			addNeptun(&str, tt.neptun)
			gotValue := str.String()

			if gotValue != tt.wantValue {
				t.Errorf("addNeptun() = %v, want %v", gotValue, tt.wantValue)
			}
		})
	}
}

func TestGitHubPublisherImagesFormatter(t *testing.T) {
	repoOwner := "org"
	repoName := "repo"
	commitHash := "aa11"
	tests := []struct {
		imageFiles []string
		wantValue  string
	}{
		{
			imageFiles: []string{},
			wantValue:  "",
		},
		{
			imageFiles: []string{"file.jpg"},
			wantValue:  "**file.jpg**\n\n![](https://github.com/org/repo/blob/aa11/file.jpg?raw=true)\n\n",
		},
		{
			imageFiles: []string{"file.jpg", "second.PNG"},
			wantValue:  "**file.jpg**\n\n![](https://github.com/org/repo/blob/aa11/file.jpg?raw=true)\n\n**second.PNG**\n\n![](https://github.com/org/repo/blob/aa11/second.PNG?raw=true)\n\n",
		},
	}
	for _, tt := range tests {
		t.Run("TestGitHubPublisherImagesFormatter", func(t *testing.T) {
			str := strings.Builder{}
			addImages(&str, tt.imageFiles, repoOwner, repoName, commitHash)
			gotValue := str.String()

			if gotValue != tt.wantValue {
				t.Errorf("addImages() = %v, want %v", gotValue, tt.wantValue)
			}
		})
	}
}

func TestGitHubPublisherDetailsResultsFormatter(t *testing.T) {
	tests := []struct {
		result    []resultsfileparser.AhkTaskResult
		wantValue string
	}{
		{
			result:    []resultsfileparser.AhkTaskResult{},
			wantValue: "",
		},
		{
			result: []resultsfileparser.AhkTaskResult{
				{
					ExerciseName: "ex1",
					TaskName:     "t1",
					Points:       12,
				},
			},
			wantValue: "**ex1 / t1**: 12\n\n\n",
		},
		{
			result: []resultsfileparser.AhkTaskResult{
				{
					ExerciseName: "ex1",
					TaskName:     "t1",
					Points:       12,
				},
				{
					ExerciseName: "ex2",
					TaskName:     "t2",
					Points:       2,
					Comment:      "comment comment",
				},
			},
			wantValue: "**ex1 / t1**: 12\n\n\n**ex2 / t2**: 2\n> comment comment\n\n",
		},
		{
			result: []resultsfileparser.AhkTaskResult{
				{
					ExerciseName: "",
					TaskName:     "t1",
					Points:       5.1,
				},
				{
					ExerciseName: "",
					TaskName:     "t2",
					Points:       1,
					Comment:      "apple apple",
				},
			},
			wantValue: "**t1**: 5.1\n\n\n**t2**: 1\n> apple apple\n\n",
		},
		{
			result: []resultsfileparser.AhkTaskResult{
				{
					ExerciseName: "",
					TaskName:     "t1",
					Points:       5.1,
				},
				{
					ExerciseName: "",
					TaskName:     "t2",
					Points:       math.NaN(),
					Comment:      "apple apple",
				},
				{
					ExerciseName: "xx",
					TaskName:     "t4",
					Points:       1.1,
				},
				{
					ExerciseName: "xx",
					TaskName:     "t5",
					Points:       3,
				},
			},
			wantValue: "**t1**: 5.1\n\n\n**t2**: N/A\n> apple apple\n\n**xx / t4**: 1.1\n\n\n**xx / t5**: 3\n\n\n",
		},
	}
	for _, tt := range tests {
		t.Run("TestGitHubPublisherDetailsResultsFormatter", func(t *testing.T) {
			str := strings.Builder{}
			addDetailedResults(&str, tt.result)
			gotValue := str.String()

			if gotValue != tt.wantValue {
				t.Errorf("addDetailedResults() = %v, want %v", gotValue, tt.wantValue)
			}
		})
	}
}

func TestGitHubPublisherSummaryFormatter(t *testing.T) {
	tests := []struct {
		result    []resultsfileparser.AhkTaskResult
		wantValue string
	}{
		{
			result:    []resultsfileparser.AhkTaskResult{},
			wantValue: "",
		},
		{
			result: []resultsfileparser.AhkTaskResult{
				{
					ExerciseName: "ex1",
					TaskName:     "t1",
					Points:       12,
				},
			},
			wantValue: "**Osszesen / Total**:\nex1: 12\n",
		},
		{
			result: []resultsfileparser.AhkTaskResult{
				{
					ExerciseName: "ex1",
					TaskName:     "t1",
					Points:       12,
				},
				{
					ExerciseName: "ex2",
					TaskName:     "t2",
					Points:       2,
				},
			},
			wantValue: "**Osszesen / Total**:\nex1: 12\nex2: 2\n",
		},
		{
			result: []resultsfileparser.AhkTaskResult{
				{
					ExerciseName: "",
					TaskName:     "t1",
					Points:       5,
				},
				{
					ExerciseName: "",
					TaskName:     "t2",
					Points:       1,
				},
			},
			wantValue: "**Osszesen / Total**:\n6\n",
		},
		{
			result: []resultsfileparser.AhkTaskResult{
				{
					ExerciseName: "",
					TaskName:     "t1",
					Points:       5,
				},
				{
					ExerciseName: "",
					TaskName:     "t2",
					Points:       math.NaN(),
				},
				{
					ExerciseName: "xx",
					TaskName:     "t4",
					Points:       1.1,
				},
				{
					ExerciseName: "xx",
					TaskName:     "t5",
					Points:       3,
				},
			},
			wantValue: "**Osszesen / Total**:\ninconclusive\nxx: 4.1\n",
		},
	}
	for _, tt := range tests {
		t.Run("TestGitHubPublisherSummaryFormatter", func(t *testing.T) {
			str := strings.Builder{}
			addSumary(&str, tt.result)
			gotValue := str.String()

			if gotValue != tt.wantValue {
				t.Errorf("addSumary() = %v, want %v", gotValue, tt.wantValue)
			}
		})
	}
}
