package publishtoapi

import (
	"crypto/rand"
	"encoding/base64"
	"math"
)

func getRandomString(l int) string {
	// based on: https://stackoverflow.com/a/55860599
	buff := make([]byte, int(math.Ceil(float64(l)/float64(1.33333333333))))
	rand.Read(buff)
	str := base64.RawURLEncoding.EncodeToString(buff)
	return str[:l]
}
