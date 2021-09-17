package imagefilesfinder

import (
	"reflect"
	"testing"
)

func TestImageFileFinder_GetImageFiles(t *testing.T) {
	tests := []struct {
		dir           string
		fileExtension string
		wantValue     []string
		wantErr       bool
	}{
		{"testfiles", ".png", []string{"IMG1.PNG", "pic2.png"}, false},
		{"testfiles", ".jpg", []string{"img3.jpg"}, false},
		{"testfiles", ".abc", []string{}, false},
	}
	for _, tt := range tests {
		t.Run("a", func(t *testing.T) {
			p := &ImageFileFinder{}
			gotValue, err := p.GetImageFiles(tt.dir, tt.fileExtension)

			if (err != nil) != tt.wantErr {
				t.Errorf("ImageFileFinder.GetImageFiles() error = %v, wantErr %v", err, tt.wantErr)
				return
			}

			if tt.wantValue == nil && gotValue != nil {
				t.Errorf("ImageFileFinder.GetImageFiles() = %v, want %v", gotValue, tt.wantValue)
			}
			if tt.wantValue != nil && !reflect.DeepEqual(gotValue, tt.wantValue) {
				t.Errorf("ImageFileFinder.GetImageFiles() = %v, want %v", gotValue, tt.wantValue)
			}
		})
	}
}
