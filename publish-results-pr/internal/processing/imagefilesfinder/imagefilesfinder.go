package imagefilesfinder

import (
	"os"
	"path/filepath"
	"sort"
	"strings"
)

type ImageFileFinder struct{}

func (p *ImageFileFinder) GetImageFiles(dir string, fileExtension string) (value []string, err error) {
	files := make([]string, 0)
	err = filepath.WalkDir(dir, func(path string, f os.DirEntry, _ error) error {
		if !f.IsDir() {
			if strings.EqualFold(filepath.Ext(path), fileExtension) {
				files = append(files, f.Name())
			}
		}
		return nil
	})

	sort.Strings(files)
	return files, err
}
