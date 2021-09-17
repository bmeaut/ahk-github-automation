FROM golang:1.17-alpine AS build

WORKDIR /src
COPY . .
RUN CGO_ENABLED=0 GOOS=linux go build -a -ldflags '-w -extldflags "-static"' -o /bin/ahkpublishresultpr

FROM scratch
COPY --from=build /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/
COPY --from=build /bin/ahkpublishresultpr /bin/ahkpublishresultpr
ENTRYPOINT ["/bin/ahkpublishresultpr"]