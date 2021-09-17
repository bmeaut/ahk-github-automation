package resultsfileparser

import (
	"errors"
	"io/ioutil"
	"log"
	"math"
	"os"
	"strconv"
	"strings"
)

type AhkTaskResult struct {
	ExerciseName string  `json:"exerciseName,omitempty"`
	TaskName     string  `json:"taskName"`
	Points       float64 `json:"points"`
	Comment      string  `json:"comment,omitempty"`
}

type ResultsFileParser struct{}

func (p *ResultsFileParser) ParseResultsFile(fileName string) (value []AhkTaskResult, err error) {
	f, err := os.Open(fileName)
	if err != nil {
		return nil, errors.New("eredmeny fajl nem talalhato - result file not found")
	}
	defer f.Close()

	rawBytes, err := ioutil.ReadAll(f)
	if err != nil {
		return nil, errors.New("eredmeny fajl nem talalhato - result file not found")
	}

	lines := strings.Split(strings.TrimPrefix(string(rawBytes), "\uFEFF"), "\n") // strip BOM and split lines
	if len(lines) == 0 {
		return nil, errors.New("eredmeny fajl ures  - result file is empty")
	}

	results := make([]AhkTaskResult, 0)
	lineIdx := 0
	for {
		if lineIdx >= len(lines) {
			break
		}

		line := lines[lineIdx]
		lineIdx = lineIdx + 1

		line = strings.TrimSpace(line)
		if len(line) == 0 {
			continue
		}

		if strings.HasPrefix(line, "###ahk#") {
			for strings.HasSuffix(line, "\\") {
				line = strings.TrimSpace(line[:len(line)-1])

				if lineIdx < len(lines) {
					nextLine := lines[lineIdx]
					lineIdx = lineIdx + 1
					line = line + "\n" + strings.TrimSpace(nextLine)
				}
			}

			// ###ahk#taskname#result#comment
			items := strings.Split(line, "#")
			items = removeEmptyStrings(items)
			if len(items) < 3 {
				log.Printf("Invalid line: %s", line)
			} else {
				results = append(results, AhkTaskResult{
					ExerciseName: getExerciseName(items[1]),
					TaskName:     getTaskName(items[1]),
					Points:       getPoints(items[2]),
					Comment:      getComments(items),
				})
			}
		} else {
			log.Printf("Invalid prefix in line: %s", line)
		}
	}

	if len(results) == 0 {
		return nil, errors.New("eredmeny fajl ures  - result file is empty")
	}

	return results, nil
}

// Gets task from exercise@task
func getTaskName(name string) string {
	if len(name) == 0 {
		return ""
	}

	idx := strings.Index(name, "@")
	if idx > -1 {
		return name[idx+1:]
	} else {
		return name
	}
}

// Gets exercise from exercise@task
func getExerciseName(name string) string {
	if len(name) == 0 {
		return ""
	}

	idx := strings.Index(name, "@")
	if idx > -1 {
		return name[0:idx]
	} else {
		return ""
	}
}

func getPoints(str string) float64 {
	if len(str) == 0 {
		return math.NaN()
	}

	if num, err := strconv.ParseFloat(str, 64); err == nil {
		return num
	}

	return math.NaN()
}

func getComments(items []string) string {
	if len(items) > 3 {
		return strings.Join(items[3:], " ")
	}
	return ""
}

func removeEmptyStrings(value []string) []string {
	result := []string{}
	for i := range value {
		if len(value[i]) > 0 {
			result = append(result, value[i])
		}
	}
	return result
}
