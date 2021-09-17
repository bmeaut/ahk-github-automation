package neptunparser

import "testing"

func TestNeptunParser_ParseNeptun(t *testing.T) {
	tests := []struct {
		fileName  string
		wantValue string
		wantErr   bool
	}{
		{"testfiles/neptun01.txt", "ABC123", false},
		{"testfiles/neptun02.txt", "ABC123", false},
		{"testfiles/NEPtun03.txt", "AB12C3", false},
		{"testfiles/neptun04.txt", "ABC123", false},
		{"testfiles/neptun05.txt", "ABC123", false},
		{"testfiles/nosuchfile", "", true},
		{"testfiles/nosuchfile.txt", "", true},
		{"testfiles/neptun10.txt", "", true},
	}
	for _, tt := range tests {
		t.Run(tt.fileName, func(t *testing.T) {
			p := &NeptunParser{}
			gotValue, err := p.ParseNeptun(tt.fileName)
			if (err != nil) != tt.wantErr {
				t.Errorf("NeptunParser.ParseNeptun() error = %v, wantErr %v", err, tt.wantErr)
				return
			}

			if tt.wantValue == "" && gotValue != nil {
				t.Errorf("NeptunParser.ParseNeptun() = %v, want nil", gotValue)
			}
			if gotValue != nil && *gotValue != tt.wantValue {
				t.Errorf("NeptunParser.ParseNeptun() = %v, want %v", gotValue, tt.wantValue)
			}
		})
	}
}
