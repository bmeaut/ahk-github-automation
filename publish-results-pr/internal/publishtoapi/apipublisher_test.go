package publishtoapi

import (
	"math"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"

	"ahk/publishresultpr/internal/processing"
	resultsfileparser "ahk/publishresultpr/internal/processing/resultsfile"
)

func TestApiPublisher_Json(t *testing.T) {
	tests := []struct {
		data      processing.AhkProcessResult
		wantValue string
	}{
		{
			data: processing.AhkProcessResult{
				GitHubRepoName:       "org/name",
				GitHubBranch:         "branch",
				GitHubPullRequestNum: 0,
				GitHubCommitHash:     "aa11cc33",
				NeptunCode:           "ABC123",
				ImageFiles:           []string{"img1.png", "img2.png"},
				Result: []resultsfileparser.AhkTaskResult{
					{
						ExerciseName: "ex1",
						TaskName:     "t1",
						Points:       2,
						Comment:      "line1 abc\nlin2 end",
					},
				},
			},
			wantValue: `{"gitHubRepoName":"org/name","gitHubBranch":"branch","gitHubCommitHash":"aa11cc33","neptunCode":"ABC123","imageFiles":["img1.png","img2.png"],"result":[{"exerciseName":"ex1","taskName":"t1","points":2,"comment":"line1 abc\nlin2 end"}]}`,
		},
		{
			data: processing.AhkProcessResult{
				GitHubRepoName:       "org/name",
				GitHubBranch:         "branch",
				GitHubPullRequestNum: 123,
				GitHubCommitHash:     "aa11cc33",
				NeptunCode:           "ABC123",
				ImageFiles:           []string{},
				Result: []resultsfileparser.AhkTaskResult{
					{
						ExerciseName: "ex1",
						TaskName:     "t1",
						Points:       2,
						Comment:      "line1 abc\nlin2 end",
					},
					{
						TaskName: "t1",
						Points:   5,
					},
				},
				Origin: "orgstr",
			},
			wantValue: `{"gitHubRepoName":"org/name","gitHubBranch":"branch","gitHubPullRequestNum":123,"gitHubCommitHash":"aa11cc33","neptunCode":"ABC123","imageFiles":[],"result":[{"exerciseName":"ex1","taskName":"t1","points":2,"comment":"line1 abc\nlin2 end"},{"taskName":"t1","points":5}],"origin":"orgstr"}`,
		},
		{
			data: processing.AhkProcessResult{
				GitHubRepoName:       "org/name",
				GitHubBranch:         "branch",
				GitHubPullRequestNum: 0,
				GitHubCommitHash:     "aa11cc33",
				NeptunCode:           "ABC123",
				ImageFiles:           []string{"img1.png", "img2.png"},
				Result: []resultsfileparser.AhkTaskResult{
					{
						ExerciseName: "ex1",
						TaskName:     "t1",
						Points:       math.NaN(),
						Comment:      "ccc",
					},
				},
			},
			wantValue: `{"gitHubRepoName":"org/name","gitHubBranch":"branch","gitHubCommitHash":"aa11cc33","neptunCode":"ABC123","imageFiles":["img1.png","img2.png"],"result":[{"exerciseName":"ex1","taskName":"t1","points":0,"comment":"ccc"}]}`,
		},
	}
	for _, tt := range tests {
		t.Run("TestApiPublisher_Json", func(t *testing.T) {

			bytes, err := SerializeResultAsJson(tt.data)
			if err != nil {
				t.Error(err)
				return
			}
			actual := string(bytes)

			if actual != tt.wantValue {
				t.Errorf("AhkProcessResult Json serialized = %v, want %v", actual, tt.wantValue)
			}
		})
	}
}

func TestApiPublisher_HasContent(t *testing.T) {
	headers := []struct {
		headerName string
		wantValue  string
	}{
		{
			headerName: "X-Ahk-Token",
			wantValue:  "xxtoken3333",
		},
		{
			headerName: "Date",
			wantValue:  "Wed, 01 Sep 2021 13:34:56 GMT",
		},
	}
	t.Run("TestApiPublisher_HasContent", func(t *testing.T) {
		ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

			if r.Method != "POST" {
				t.Errorf("Expected POST request, got %s", r.Method)
			}

			if _, found := r.Header["X-Ahk-Delivery"]; !found {
				t.Errorf("Missing X-Ahk-Delivery header from request")
			}

			for _, h := range headers {
				actual := r.Header[h.headerName][0]
				if actual != h.wantValue {
					t.Errorf("Http header %v = %v, want %v", h.headerName, actual, h.wantValue)
				}
			}
		}))
		defer ts.Close()

		sender := NewApiPublisher()
		err := sender.Publish(processing.AhkProcessResult{}, Config{
			Url:    ts.URL,
			Token:  "xxtoken3333",
			Secret: "secret",
			Date:   time.Date(2021, time.September, 1, 13, 34, 56, 0, time.UTC),
		})
		if err != nil {
			t.Error(err)
		}
	})
}

func TestApiPublisher_ErrorIfNot200Ok(t *testing.T) {
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusServiceUnavailable)
	}))
	defer ts.Close()

	sender := NewApiPublisher()
	err := sender.Publish(processing.AhkProcessResult{}, Config{Url: ts.URL, Token: "token"})
	if err == nil {
		t.Error("failed request should have reported error")
	}
}
