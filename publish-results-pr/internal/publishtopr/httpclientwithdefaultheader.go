package publishtopr

import "net/http"

type wrappedTransport struct {
	http.Header
	rt http.RoundTripper
}

func (h wrappedTransport) RoundTrip(req *http.Request) (*http.Response, error) {
	for k, v := range h.Header {
		req.Header[k] = v
	}
	return h.rt.RoundTrip(req)
}

func getHttpClientWithHeader(name, value string) *http.Client {
	client := http.DefaultClient

	transport := client.Transport
	if transport == nil {
		transport = http.DefaultTransport
	}

	mewTransport := wrappedTransport{
		Header: make(http.Header),
		rt:     transport,
	}
	mewTransport.Header.Set(name, value)

	client.Transport = mewTransport
	return client
}
