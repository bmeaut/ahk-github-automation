package publishtoapi

import "time"

type Config struct {
	Url    string
	Token  string
	Secret string
	Date   time.Time
}
