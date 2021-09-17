package main

import (
	"log"
	"os"
	"time"

	"ahk/publishresultpr/internal/appargs"
	"ahk/publishresultpr/internal/processing"
	"ahk/publishresultpr/internal/publishtoapi"
	"ahk/publishresultpr/internal/publishtopr"
)

func main() {
	log.Println("AHK Publish Result to GitHub PR")

	log.Println("Reading args...")
	argReader := appargs.NewGitHubActionArgsReader()
	appArgs, err := argReader.GetArgs()
	if err != nil {
		log.Fatal(err)
	}
	log.Println("Reading args... done.")

	dir, _ := os.Getwd()
	log.Printf("Working directory is: %s\n", dir)

	log.Println("Processing...")
	processor := processing.NewProcessor()
	result, err := processor.Process(*appArgs, dir)
	if err != nil {
		log.Fatal(err)
	}
	log.Println("Processing... done.")

	log.Println("Publishing results to PR...")
	prpub := publishtopr.NewPrPublisher()
	prpubConfig := publishtopr.Config{
		GitHubToken: appArgs.GitHubToken,
		RepoOwner:   appArgs.GitHubRepoOwner,
		RepoName:    appArgs.GitHubRepoName,
		CommitHash:  appArgs.GitCommitHash,
		PrNumber:    appArgs.GitHubPullRequestNum,
	}
	err = prpub.Publish(result, prpubConfig)
	if err != nil {
		log.Fatal(err)
	}
	log.Println("Publishing results to PR... done.")

	log.Println("Sending result to Ahk Api...")
	apipub := publishtoapi.NewApiPublisher()
	apipubConfig := publishtoapi.Config{
		Url:    appArgs.AhkAppUrl,
		Token:  appArgs.AhkAppToken,
		Secret: appArgs.AhkAppSecret,
		Date:   time.Now(),
	}
	err = apipub.Publish(result, apipubConfig)
	if err != nil {
		log.Fatal(err)
	}
	log.Println("Sending result to Ahk Api... done.")

	log.Println("Finished. Bye.")
}
