package publishtopr

import (
	"ahk/publishresultpr/internal/processing"
	"context"
	"fmt"
	"log"
	"net/http/httputil"

	"github.com/google/go-github/v39/github"
)

type prPublisher struct{}

type PrPublisher interface {
	Publish(result processing.AhkProcessResult, config Config) error
}

func NewPrPublisher() PrPublisher {
	return new(prPublisher)
}

func (s *prPublisher) Publish(result processing.AhkProcessResult, config Config) (err error) {
	h := getHttpClientWithHeader("Authorization", fmt.Sprintf("token %s", config.GitHubToken))
	client := github.NewClient(h)

	var commentText = createComment(result.NeptunCode, result.ImageFiles, result.Result, config.RepoOwner, config.RepoName, config.CommitHash)
	err = addComment(client, config.RepoOwner, config.RepoName, config.PrNumber, commentText)
	if err != nil {
		return err
	}

	err = removeLabels(client, config.RepoOwner, config.RepoName, config.PrNumber)
	if err != nil {
		return err
	}

	return err
}

func addComment(client *github.Client, repoOwner, repoName string, prNum int, commentText string) error {
	comment := github.IssueComment{
		Body: &commentText,
	}

	log.Printf("Sending comment to %s/%s PR %d", repoOwner, repoName, prNum)
	createdComment, resp, err := client.Issues.CreateComment(context.Background(), repoOwner, repoName, prNum, &comment)
	if err != nil {
		d, _ := httputil.DumpResponse(resp.Response, true)
		log.Printf("Response indicates an error\n%s", string(d))
		return fmt.Errorf("adding comment to GitHub failed: %w", err)
	}

	log.Printf("Created comment with id %d at %s\n", *createdComment.ID, *createdComment.HTMLURL)
	return nil
}

func removeLabels(client *github.Client, repoOwner, repoName string, prNum int) error {
	log.Printf("Removing labels from %s/%s PR %d", repoOwner, repoName, prNum)
	resp, err := client.Issues.RemoveLabelsForIssue(context.Background(), repoOwner, repoName, prNum)
	if err != nil {
		d, _ := httputil.DumpResponse(resp.Response, true)
		log.Printf("Response indicates an error\n%s", string(d))
		return fmt.Errorf("adding comment to GitHub failed: %w", err)
	}

	log.Printf("Removed labels")
	return nil
}
