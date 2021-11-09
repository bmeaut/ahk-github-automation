package publishtoapi

import (
	"bytes"
	"crypto/hmac"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"log"
	"math"
	"net/http"
	"net/http/httputil"
	"strings"
	"time"

	"ahk/publishresultpr/internal/processing"
)

type apiPublisher struct{}

type ApiPublisher interface {
	Publish(result processing.AhkProcessResult, config Config) error
}

func NewApiPublisher() ApiPublisher {
	return new(apiPublisher)
}

func SerializeResultAsJson(result processing.AhkProcessResult) ([]byte, error) {
	// marshaler does not tolerate NaN
	fixupData(&result)
	jsonStr, err := json.Marshal(result)
	return jsonStr, err
}

func (s *apiPublisher) Publish(result processing.AhkProcessResult, config Config) error {

	jsonStr, err := SerializeResultAsJson(result)
	if err != nil {
		return err
	}

	req, err := http.NewRequest(http.MethodPost, config.Url, bytes.NewBuffer(jsonStr))
	if err != nil {
		return err
	}

	hmacsig := getHmacSignature(string(jsonStr), http.MethodPost, config.Url, config.Secret, config.Date)

	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Ahk-Token", config.Token)
	req.Header.Set("X-Ahk-Sha256", hmacsig)
	req.Header.Set("X-Ahk-Delivery", getRandomString(16))
	req.Header.Set("Date", config.Date.Format(http.TimeFormat))

	log.Printf("Sending HTTP %s to %s\n", req.Method, req.URL)
	log.Printf("Token: %s*****\n", config.Token[:4])
	log.Printf("Sig: %s*****\n", hmacsig[:4])
	log.Printf("Date: %s\n", config.Date.Format(http.TimeFormat))

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	log.Println("Request sent.")

	if resp.StatusCode < 200 || resp.StatusCode >= 300 {
		d, _ := httputil.DumpResponse(resp, true)
		log.Printf("Response indicates an error\n%s", string(d))
		return fmt.Errorf("sending data to server was not successful, received status code %d", resp.StatusCode)
	}

	log.Println("Response indicates success.")
	return nil
}

func getHmacSignature(payload, httpverb, httpurl, secret string, date time.Time) string {
	h := hmac.New(sha256.New, []byte(secret))
	h.Write([]byte(strings.ToUpper(httpverb) + "\n"))
	h.Write([]byte(strings.ToLower(httpurl) + "\n"))
	h.Write([]byte(date.Format(http.TimeFormat) + "\n"))
	h.Write([]byte(payload))
	return base64.StdEncoding.EncodeToString(h.Sum(nil))
}

func fixupData(data *processing.AhkProcessResult) {
	if data.Result != nil {
		for i := 0; i < len(data.Result); i++ {
			if math.IsNaN(data.Result[i].Points) {
				data.Result[i].Points = 0
			}
		}
	}
}
