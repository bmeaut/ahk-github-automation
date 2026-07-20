# AHK Frontend

Angular 21 SPA (teacher portal) for `ahk.aut.bme.hu`, talking to **ahk-backend**. Standalone
components + signals. Cookie-based auth; in dev the SPA and API are made same-origin by a proxy so
**no CORS** is needed.

> Skeleton milestone: login (local + OIDC), a host/admin course-management screen, and a
> course-scoped dashboard that exercises course isolation. Real dashboards land in the port.

## Structure

- `src/app/api/` — **NSwag-generated** TypeScript clients + DTOs (do not edit by hand; run
  `npm run generate-api`).
- `src/app/core/auth/` — `AuthService` (session signals), functional guards, HTTP interceptor.
- `src/app/core/course/` — active-course context + course membership guard.
- `src/app/layout/shell/` — authenticated app frame with course switcher.
- `src/app/features/` — `login/`, `admin/courses/`, `course/dashboard/`.

## Run (dev, HTTPS)

```bash
npm install
npm start          # ng serve --ssl on https://localhost:4200, proxying /api → https://localhost:7443
```

Start **ahk-backend** first (https://localhost:7443). Log in with a seeded account, e.g.
`admin` / `Admin123!` or `instructor` / `Instructor123!`.

## Regenerate the API client

```bash
# ahk-backend must be running (its OpenAPI doc is the source)
npm run generate-api      # NSwag reads https://localhost:7443/swagger/v1/swagger.json → src/app/api/api-client.ts
```

Note: `nswag.json` uses `"runtime": "Net100"` to match the installed .NET 10 SDK.

## Build / test

```bash
npm run build
npm test
```

## Dev proxy & auth notes

- `proxy.conf.js` forwards `/api/*` to the backend over HTTPS (`secure: false` for the self-signed
  dev cert). `API_BASE_URL` is provided as `''` so the generated clients issue **relative** requests
  (same-origin → cookie flows, no CORS).
- OIDC login is a full-page navigation to `/api/auth/external/challenge` (proxied to the backend),
  which runs the standard redirect flow and issues the Identity cookie.
