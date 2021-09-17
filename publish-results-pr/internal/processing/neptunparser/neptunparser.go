package neptunparser

import (
	"errors"
	"io/ioutil"
	"os"
	"regexp"
	"strings"
)

type NeptunParser struct{}

func (p *NeptunParser) ParseNeptun(fileName string) (value *string, err error) {
	f, err := os.Open(fileName)
	if err != nil {
		return nil, errors.New("neptun.txt nem talalhato - neptun.txt does not exist")
	}
	defer f.Close()

	rawBytes, err := ioutil.ReadAll(f)
	if err != nil {
		return nil, errors.New("neptun.txt nem talalhato - neptun.txt does not exist")
	}

	lines := strings.Split(string(rawBytes), "\n")
	if len(lines) == 0 {
		return nil, errors.New("neptun.txt ures - neptun.txt is empty")
	}

	neptun := strings.TrimSpace(lines[0])
	if len(neptun) == 0 {
		return nil, errors.New("neptun.txt ures - neptun.txt is empty")
	}

	matched, _ := regexp.MatchString("^[a-zA-Z0-9]{6}$", neptun)
	if !matched {
		return nil, errors.New("neptun.txt ervenytelen neptun kodot tartalmaz - neptun.txt contains an invalid neptun code")
	}

	neptun = strings.ToUpper(neptun)
	return &neptun, nil
}
