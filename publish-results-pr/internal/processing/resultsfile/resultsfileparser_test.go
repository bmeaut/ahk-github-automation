package resultsfileparser

import (
	"reflect"
	"testing"
)

func TestResultsFileParser_ParseResultsFile(t *testing.T) {
	tests := []struct {
		fileName  string
		wantValue []AhkTaskResult
		wantErr   bool
	}{
		{
			fileName: "testfiles/doesnotexist.txt",
			wantErr:  true,
		},
		{
			fileName: "testfiles/empty.txt",
			wantErr:  true,
		},
		{
			fileName: "testfiles/res1.txt",
			wantErr:  false,
			wantValue: []AhkTaskResult{
				{
					ExerciseName: "",
					TaskName:     "ex1",
					Points:       2,
				},
			},
		},
		{
			fileName: "testfiles/res2.txt",
			wantErr:  false,
			wantValue: []AhkTaskResult{
				{
					ExerciseName: "",
					TaskName:     "ex1",
					Points:       2,
				},
			},
		},
		{
			fileName: "testfiles/res3.txt",
			wantErr:  false,
			wantValue: []AhkTaskResult{
				{
					ExerciseName: "ex1",
					TaskName:     "t1",
					Points:       2.5,
				},
				{
					ExerciseName: "ex1",
					TaskName:     "t2",
					Points:       3,
					Comment:      "comment",
				},
			},
		},
		{
			fileName: "testfiles/res4.txt",
			wantErr:  false,
			wantValue: []AhkTaskResult{
				{
					ExerciseName: "ex1",
					TaskName:     "t1",
					Points:       2,
					Comment:      "line1\nline2 abc\nline3 end",
				},
				{
					ExerciseName: "ex1",
					TaskName:     "t2",
					Points:       3,
					Comment:      "comment",
				},
				{
					ExerciseName: "",
					TaskName:     "ex3",
					Points:       0,
					Comment:      "line1\nline2 abc\nline3 end",
				},
			},
		},
	}
	for _, tt := range tests {
		t.Run(tt.fileName, func(t *testing.T) {
			p := &ResultsFileParser{}
			gotValue, err := p.ParseResultsFile(tt.fileName)
			if (err != nil) != tt.wantErr {
				t.Errorf("ResultsFileParser.ParseResultsFile() error = %v, wantErr %v", err, tt.wantErr)
				return
			}
			if !reflect.DeepEqual(gotValue, tt.wantValue) {
				t.Errorf("ResultsFileParser.ParseResultsFile() = %v, want %v", gotValue, tt.wantValue)
			}
		})
	}
}
