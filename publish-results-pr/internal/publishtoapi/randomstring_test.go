package publishtoapi

import (
	"testing"
)

func TestGenerateRandomString(t *testing.T) {
	for l := 1; l < 32; l++ {
		s := getRandomString(l)
		if len(s) != l {
			t.Error("wrong length")
		}
	}
}
