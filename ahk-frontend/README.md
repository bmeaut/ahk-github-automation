# AHK Frontend

Angular 21 SPA (teacher portal) for `ahk.aut.bme.hu`, talking to **ahk-backend**. Standalone
components + signals. Cookie-based auth; in dev the SPA and API are made same-origin by a proxy so
**no CORS** is needed.

> Screens today: sign-in (local + BME SSO), the site administration console (courses, per-course
> GitHub integration, CI callback tokens, staff, users, health), and the course submissions dashboard.

## Structure

- `src/app/api/` — **NSwag-generated** TypeScript clients + DTOs (do not edit by hand; run
  `npm run generate-api`).
- `src/app/core/auth/` — `AuthService` (session signals), functional guards, HTTP interceptor.
- `src/app/core/course/` — active-course context + course membership guard.
- `src/app/layout/shell/` — authenticated app frame: topbar with the course switcher, and a rail whose
  contents follow the context (course screens vs. site administration).
- `src/app/shared/health-chain/` — a course's health drawn as the pipeline it describes.
- `src/app/features/` — `login/`, `no-access/`, `course/dashboard/`, and `admin/` (`courses/` with the
  course editor, `users/`, `health/`).
- `src/styles.scss` — the design system, in the **BME AUT visual identity** (crimson Georgia headings,
  Verdana body, parchment table headers). Component stylesheets are Angular-scoped, so tokens and the
  shared classes (`page`, `card`, `field`, `btn`, `table.data`, `badge`, `notice`) all live here. Reach
  for those before writing new component CSS. `--brand`/`--link`/`--bad` are three different reds on
  purpose (see the comments in the file).
- `public/` — static assets served at `/`, including the department logos (`bme-aut-logo*.png`) and the
  `eduid-logo.png` on the login button. These are derived copies; the masters and the palette's
  provenance are in `brand/` (BME AUT identity pack + eduID logo), alongside this README.

## Run (dev, HTTPS)

```bash
npm install
npm start          # ng serve --ssl on https://localhost:4200, proxying /api → https://localhost:7443
```

Start **ahk-backend** first (https://localhost:7443). Log in with a seeded account, e.g.
`admin` / `Admin123!` or `instructor` / `Instructor123!`.

A single run: `npx ng test --watch=false`.

## Signing in

**eduID leads.** The login screen's primary action is the eduID button (a full-page navigation to
`/api/auth/external/challenge`); the local username/password form is collapsed behind an "I don't have
an eduID account" link and only appears when asked for. The button follows the
[eduID brand](https://eduid.hu/hu/depo/) but is rendered from the eduID logo plus an English "Login"
rather than shipping their Hungarian-label PNG. Its `--eduid` blue is scoped to `login.scss` and is
deliberately absent from the global palette.

## Who sees what

- **Site admins** land on `/admin/courses` and can open *any* course's instructor screens — the API
  lists every course in `GET /api/auth/me` for them, so the course switcher and `courseGuard` need no
  special case. Courses reached that way are marked in the rail, since the admin is not assigned staff.
- **Instructors** land on their first course. A signed-in user with no course assignment lands on
  `/no-access`, which says who to ask, rather than being bounced back to the login form.

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
