package publishtoapi

import (
	"testing"
	"time"
)

const secret string = "Wcks02cnncc67c33"
const httpverb string = "POST"
const httpurl string = "https://my.url.com/address"

func TestHmacSignature(t *testing.T) {
	tests := []struct {
		data      string
		wantValue string
	}{
		{
			data:      "aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n",
			wantValue: "SGAhL9hfzLqi30G1uqtQyErRC4oKBlxT9NImaJ/V9CQ=",
		},
		{
			data:      "qqqq\r\nsdfsdfsdfsdf\r\nwwwwwwwwwwwww\r\n",
			wantValue: "K7lZXguubpUONKhHh40lAzxt2vPyZnm6LkjLhrYPwAo=",
		},
		{
			data:      "aaaaaaqqqqqqqqqqqqqqq",
			wantValue: "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=",
		},
	}
	for _, tt := range tests {
		t.Run("TestHmacSignature", func(t *testing.T) {

			date := time.Date(2021, time.September, 1, 13, 34, 56, 0, time.UTC)

			gotValue := getHmacSignature(string(tt.data), httpverb, httpurl, secret, date)
			if gotValue != tt.wantValue {
				t.Errorf("getHmacSignature() = %v, want %v", gotValue, tt.wantValue)
			}

			sigWrongVerb := getHmacSignature(string(tt.data), "wrongverb", httpurl, secret, date)
			if sigWrongVerb == tt.wantValue {
				t.Errorf("getHmacSignature() should use HttpVerb in calculating hash")
			}

			sigWrongUrl := getHmacSignature(string(tt.data), httpverb, "wrongurl", secret, date)
			if sigWrongUrl == tt.wantValue {
				t.Errorf("getHmacSignature() should use url in calculating hash")
			}

			sigWrongPayload := getHmacSignature(string(tt.data)+"data", httpverb, httpurl, secret, date)
			if sigWrongPayload == tt.wantValue {
				t.Errorf("getHmacSignature() should use payload in calculating hash")
			}

			sigWrongDate := getHmacSignature(string(tt.data), httpverb, httpurl, secret, date.Add(time.Second))
			if sigWrongDate == tt.wantValue {
				t.Errorf("getHmacSignature() should use date in calculating hash")
			}
		})
	}
}
