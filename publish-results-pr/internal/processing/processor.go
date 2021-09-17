package processing

import (
	"fmt"
	"log"

	"ahk/publishresultpr/internal/appargs"
	"ahk/publishresultpr/internal/processing/imagefilesfinder"
	"ahk/publishresultpr/internal/processing/neptunparser"
	resultsfileparser "ahk/publishresultpr/internal/processing/resultsfile"
)

type Processor interface {
	Process(appArgs appargs.AppArgs, workingDir string) (res AhkProcessResult, err error)
}

type processor struct {
}

func NewProcessor() Processor {
	return new(processor)
}

func (p *processor) Process(appArgs appargs.AppArgs, workingDir string) (res AhkProcessResult, err error) {
	neptunParser := new(neptunparser.NeptunParser)
	neptun, err := neptunParser.ParseNeptun(appArgs.NeptunFileName)
	if err != nil {
		return AhkProcessResult{}, err
	}
	log.Printf("Neptun: %s\n", *neptun)

	imageFilesFinder := new(imagefilesfinder.ImageFileFinder)
	imageFiles, err := imageFilesFinder.GetImageFiles(workingDir, appArgs.ImageExtension)
	if err != nil {
		return AhkProcessResult{}, err
	}
	for _, imageFile := range imageFiles {
		log.Printf("Found image file: %s\n", imageFile)
	}

	resultsFileParser := new(resultsfileparser.ResultsFileParser)
	results, err := resultsFileParser.ParseResultsFile(appArgs.ResultFile)
	if err != nil {
		return AhkProcessResult{}, err
	}
	for _, r := range results {
		log.Printf("Result for %s %s %f\n", r.ExerciseName, r.TaskName, r.Points)
	}

	return AhkProcessResult{
		GitHubRepoName:       appArgs.GitHubRepoFullName,
		GitHubBranch:         appArgs.GitHubBranch,
		GitHubPullRequestNum: appArgs.GitHubPullRequestNum,
		GitHubCommitHash:     appArgs.GitCommitHash,
		NeptunCode:           *neptun,
		ImageFiles:           imageFiles,
		Result:               results,
		Origin:               fmt.Sprintf("https://github.com/%s/commit/%s https://github.com/%s/actions/runs/%s", appArgs.GitHubRepoFullName, appArgs.GitCommitHash, appArgs.GitHubRepoFullName, appArgs.GitHubActionRunId),
	}, nil
}
