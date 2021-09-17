package publishtopr

import (
	resultsfileparser "ahk/publishresultpr/internal/processing/resultsfile"
	"fmt"
	"math"
	"path"
	"sort"
	"strconv"
	"strings"
)

const markdown_newline = "\n"

func createComment(neptun string, imageFiles []string, taskResults []resultsfileparser.AhkTaskResult, repoOwner, repoName, commitHash string) string {
	str := strings.Builder{}

	addImages(&str, imageFiles, repoOwner, repoName, commitHash)
	addNeptun(&str, neptun)
	str.WriteString(markdown_newline + markdown_newline)
	addDetailedResults(&str, taskResults)
	addSumary(&str, taskResults)

	return str.String()
}

func addImages(str *strings.Builder, imageFiles []string, repoOwner, repoName, commitHash string) {
	if len(imageFiles) > 0 {
		for _, r := range imageFiles {
			f := path.Base(r)
			str.WriteString("**" + f + "**" + markdown_newline + markdown_newline)
			str.WriteString(fmt.Sprintf("![](https://github.com/%s/%s/blob/%s/%s?raw=true)", repoOwner, repoName, commitHash, f))
			str.WriteString(markdown_newline + markdown_newline)
		}
	}
}

func addNeptun(str *strings.Builder, neptun string) {
	if len(neptun) > 0 {
		str.WriteString("**Neptun**: " + neptun)
	} else {
		str.WriteString("**Neptun**: :exclamation: N/A")
	}
}

func addDetailedResults(str *strings.Builder, taskResults []resultsfileparser.AhkTaskResult) {
	if len(taskResults) == 0 {
		return
	}

	for _, r := range taskResults {
		str.WriteString("**" + formatTaskName(r) + "**: ")
		if math.IsNaN(r.Points) {
			str.WriteString("N/A")
		} else {
			str.WriteString(strconv.FormatFloat(r.Points, 'f', -1, 64))
		}
		str.WriteString(markdown_newline)
		if len(r.Comment) > 0 {
			str.WriteString("> " + r.Comment)
		}
		str.WriteString(markdown_newline + markdown_newline)
	}
}

func addSumary(str *strings.Builder, taskResults []resultsfileparser.AhkTaskResult) {
	if len(taskResults) == 0 {
		return
	}

	str.WriteString("**Osszesen / Total**:")
	str.WriteString(markdown_newline)

	groupByExercise := map[string]float64{}
	for _, r := range taskResults {
		groupByExercise[r.ExerciseName] += r.Points
	}

	// deterministic ordering of items require explicit sorting
	exNamesSorted := make([]string, 0)
	for name := range groupByExercise {
		exNamesSorted = append(exNamesSorted, name)
	}
	sort.Strings(exNamesSorted)

	for _, name := range exNamesSorted {
		value := groupByExercise[name]
		if len(name) > 0 {
			str.WriteString(name + ": ")
		}

		if math.IsNaN(value) {
			str.WriteString("inconclusive")
		} else {
			str.WriteString(strconv.FormatFloat(value, 'f', -1, 64))
		}

		str.WriteString(markdown_newline)
	}
}

func formatTaskName(r resultsfileparser.AhkTaskResult) string {
	taskName := r.TaskName
	if len(taskName) == 0 {
		taskName = "N/A"
	}

	if len(r.ExerciseName) > 0 {
		return fmt.Sprintf("%s / %s", r.ExerciseName, taskName)
	} else {
		return taskName
	}
}
