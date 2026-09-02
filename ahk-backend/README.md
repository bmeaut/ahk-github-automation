# AHK Backend

Centralized ASP.NET Core (.NET 10) Web API for the AHK portal at `ahk.aut.bme.hu`. Replaces the
per-course Azure Functions deployments with a single site whose domain data is assigned to
**Courses**. Provides ASP.NET Identity authentication (local username/password + generic OIDC,
cookie-based) and course-scoped, membership-authorized endpoints.

> Milestone status: feature-complete against the original system. Auth, course-scoping, the domain
> model and services, the read endpoints, the site administration surface, **assignments** (the GitHub
> Classroom replacement) and now the write-side entry points — the **GitHub webhook receiver**, **`/ahk
> ok` chatops** and the **HMAC-verified CI callback** — are all in place, so a course can run on the
> portal with no Azure Functions. The four original apps remain in the repository, deployed, for
> courses that have not migrated yet; see [Cutover per course](docs/github-app.md#cutover-per-course).

## Machine-to-machine endpoints

Two routes are called by machines rather than by the SPA. Both are anonymous — a signature is the
authentication — and both are excluded from the OpenAPI document, so no TypeScript client is generated
for them.

| Route | Authenticated by | Resolves its course from |
|---|---|---|
| `POST /api/integrations/github` | `X-Hub-Signature-256`, against the course's own webhook secret | `repository.full_name` in the payload (organization, then repo-name prefix) |
| `POST /api/integrations/evaluation-result` | `X-Ahk-Sha256` over verb + URL + date + body | the `X-Ahk-Token` header — the authenticated credential, not the caller-supplied repository name |

Neither has a `{course}` path segment, which is why `ICourseResolutionService` exists. See
[docs/github-app.md](docs/github-app.md) and [docs/ci-callback.md](docs/ci-callback.md).

⚠️ The webhook secret is per course, and the only thing identifying the course is the repository name
*inside* the body — so the receiver has to parse an unverified payload before it can verify it.
Everything before the signature check is deliberately inert: one property is read from a `JsonDocument`
that is then dropped, two indexed reads happen, and nothing is written, logged or called until the HMAC
passes. Keep it that way.

## Projects

- **Ahk.Web.Server** — Web API host: `Program.cs` wiring, `Auth/` (local + OIDC controllers),
  `Admin/` (host-context course, user and health management), `Courses/` (course-context endpoints),
  `CourseContext/` (course resolution middleware + membership authorization).
- **Ahk.Web.Services** — domain logic: grading, status tracking, submission resolution, course
  resolution, CI callback tokens, `Assignments/` (assignment administration and the student invite
  flow), `GitHub/` (the App installation-token provider and the REST calls it authenticates),
  `GitHubWebhooks/` (the webhook dispatcher and the ten rule/status/chatops handlers ported from
  `github-monitor`), `Integrations/` (the CI callback's HMAC scheme), and `Health/` (the course
  health checks).

  **Octokit** is the portal's GitHub API client — one client, built by `ICourseGitHubClientFactory`.
  The single exception is `CourseGitHubAppTokenProvider`, which stays on a raw `HttpClient`: it signs
  an App JWT and exchanges it for an installation token, which is the auth bootstrap rather than an
  API call, and moving it would change the shape of the permissions the health check reads.
- **Ahk.Web.Data** — EF Core `ApplicationDbContext` (Identity + `Course`/`CourseMembership` +
  `ICourseScoped` global query filter), migrations, and the dev data seeder.
- **Ahk.Web.Server.Tests** — xUnit tests (course-scoping unit tests, health-check tests, grade parity
  tests + API smoke tests via `WebApplicationFactory`).

## Run

```bash
# 1. Create/upgrade the database (LocalDB by default; see appsettings.Development.json)
dotnet ef database update --project Ahk.Web.Data --startup-project Ahk.Web.Data

# 2. Run the API over HTTPS (Swagger UI at /swagger, OpenAPI at /swagger/v1/swagger.json)
dotnet run --project Ahk.Web.Server --launch-profile https   # https://localhost:7443

# Tests
dotnet test
```

Dev data seeder (Development env only) creates:

- site admin — `admin` / `Admin123!`
- instructor (member of `viaubc01` only) — `instructor` / `Instructor123!`
- course admin of `viaubc01` — `courseadmin` / `CourseAdmin123!` — no site role, but gets "Manage course":
  statistics, health, read-only settings and full staff management
- sample courses `viaubc01`, `viaubb01` — deliberately configured differently so the health dashboard
  shows a mix of states

## Configuration (keys under `Authentication`/`ConnectionStrings`)

- `ConnectionStrings:Default` — MSSQL connection string.
- `Authentication:Oidc:*` — OIDC provider. When `Authority`/`ClientId` are empty, OIDC is disabled and
  the app runs with local login only.

### OIDC against the BME IdP

Registered as client `AUTAhkClient` at `https://idp.bme.hu` (Shibboleth). The defaults in
`Configuration/OidcOptions.cs` encode constraints of that provider — changing them breaks login:

| Setting | Value | Why |
|---|---|---|
| `Scopes` | `openid email userinfo` | Exactly what is registered; `profile` is **not** registered |
| `UsePkce` | `false` | IdP does not advertise `code_challenge_methods_supported` |
| `ResponseMode` | `query` | `form_post` would drop the correlation cookie (SameSite=Lax) |
| `EndSessionEndpoint` | empty | IdP advertises none → logout is local-only |

`neptun_code` and `eduperson_scoped_affiliation` arrive from the **userinfo** endpoint and are mapped
explicitly in `Program.cs` (ASP.NET maps no standard claims by default), then persisted onto
`ApplicationUser` by `Auth/ExternalClaimsMapper.cs` on every login.

**The client secret is never committed**: use `dotnet user-secrets set "Authentication:Oidc:ClientSecret" "…"`
locally and the `Authentication__Oidc__ClientSecret` environment variable in production.

### Local OIDC development

Only the production redirect URI (`https://ahk.aut.bme.hu/signin-oidc`) is registered, so localhost
cannot talk to the real IdP. Development instead uses the in-app mock provider (`MockOidc/`), enabled by
`Authentication:Oidc:UseMockProvider` — it serves discovery/authorize/token/userinfo/JWKS under
`/mock-oidc` and signs real RS256 id_tokens, so the genuine validation path is exercised.

```bash
# choose which fixed persona the next login returns
curl -k "https://localhost:7443/mock-oidc/persona?user=student"   # instructor | student | noclaims
```

## Site administration API

All under `/api/admin/...`, all requiring the site-level `Admin` role. This is the surface the SPA's
admin console is built on.

| Route | Purpose |
|---|---|
| `GET/POST /courses`, `GET/PUT/DELETE /courses/{id}` | The course register. `DELETE` requires `?confirmSlug=` to match, and cascades to the course's students, submissions, events, grades, tokens and staff |
| `GET/PUT /courses/{id}/github` | GitHub integration: App ID + private key, access token, webhook secret, workflow-run limit, on/off |
| `GET/PUT/DELETE /courses/{id}/members[/{userId}]` | Course staff and the role each holds |
| `GET/POST/DELETE /courses/{id}/tokens[/{tokenId}]` | CI callback tokens. The creating response is the only place a token's secret appears |
| `GET /users`, `POST /users`, `GET/PUT/DELETE /users/{id}` | Accounts. Search by name/username/email/Neptun, filter by course |
| `PUT /users/{id}/roles` | Site roles. An admin cannot remove their own `Admin` role |
| `PUT/DELETE /users/{id}/courses[/{courseId}]` | Course assignments |
| `POST /users/{id}/password` | Set a local account's password |
| `GET /health`, `GET /health/{courseId}` | Run the course health checks live |
| `POST /health/refresh-stale` | Queue a background re-check of every course whose cached verdict is past its TTL. Returns 202 at once; runs nothing itself |

**Stored credentials are never returned.** The GitHub config DTO reports only whether each credential
is present (plus a last-four hint for the access token). On update, each credential field follows one
rule: `null` leaves the stored value alone, `""` clears it, anything else replaces it — so saving an
untouched form cannot wipe a secret the browser was never shown.

## Course health checks

`Ahk.Web.Services/Health/` answers "is this course's integration actually wired up?". Each check is an
`ICourseHealthCheck` registered in DI and discovered by `CourseHealthService`, so **adding a check is
one class plus one registration line** — the controller and the UI need no change.

| Check | What it verifies |
|---|---|
| `github-webhook-config` | The course has an organization to route deliveries from, a secret to validate their signature, and the integration is on. Local |
| `github-access-token` | `GET /user` and `GET /orgs/{org}` against api.github.com with the stored token. Network, 10s timeout |
| `github-app-installation` | The App credentials mint an installation token, the installation covers **all** repositories, and it was granted `administration: write` — without which assignments cannot create repositories. Network, 10s timeout |
| `ci-callback-token` | At least one non-revoked `CourseWebhookToken` exists, so evaluation results are still accepted |

Results carry a status (`Healthy` / `Warning` / `Failed` / `NotConfigured`), a message, and a
remediation line; a course's overall status is the worst of its checks. Checks must not throw — a
failure is a `Failed` result, so one unreachable course cannot take the dashboard down.

### The cached verdict

A full run is sequential and two of the checks call GitHub with a ten-second budget each, so
`GET /admin/health` costs roughly `N_courses × 30s`. Only the health dashboard pays that: it runs live
on every open, which is the point — an admin opens it having just changed a credential.

Every run also stamps the outcome onto the `Course` row — `HealthStatus`, `HealthCheckedAt` and
`HealthSummary` (the comma-joined titles of the checks that did not pass, deliberately without their
messages). The course register reads only that, in the same query that lists the courses, so it paints
at once and never touches GitHub.

A cached verdict older than `Health:CacheTtl` (24 hours) is **still shown** — stale beats blank — and
the register asks for a refresh with `POST /admin/health/refresh-stale`, which enqueues the stale
course ids on `ICourseHealthRefreshQueue` and returns. `CourseHealthRefreshWorker` drains that queue
one course at a time, re-reading each course's timestamp first so anything already refreshed is
skipped; the refreshed verdict appears on the next page load. The worker is switched off in tests via
`Health:RefreshWorkerEnabled`.

## Assignments (the GitHub Classroom replacement)

Instructors define assignments against a template repository and hand out an invite link; students
follow it and the portal creates their repository. **`docs/github-app.md` is the reference for the
GitHub App this depends on** — registration, permissions, installation scope and troubleshooting.

| Route | Authorization | Purpose |
|---|---|---|
| `/api/{course}/assignments` | `CourseMember` | Instructor CRUD, archive/unarchive, regenerate invite link, acceptance roster |
| `/api/{course}/invite/{token}` | **`[Authorize]` only** | The student flow: state, then `POST accept` |
| `/api/my/assignments` | `[Authorize]` | Every repository the caller holds, across courses, plus `POST {id}/resend-invitation` |
| `/api/profile/github` | `[Authorize]` | Records the caller's GitHub username after verifying it exists |

Three things about this are deliberate and easy to break:

- **The invite and `/my` endpoints must not require course membership.** Accepting an assignment is
  how a student first appears in a course at all, so gating them on `CourseMember` locks every
  student out of the only endpoints meant for them. The invite *token* is the capability.
- **`/api/my/...` has no `{course}` segment**, so no current course is resolved and the course query
  filter matches nothing. `StudentAssignmentService` reads with `IgnoreQueryFilters()` and filters on
  the user id; a "helpful" cleanup that removes those calls silently returns zero rows.
- **Assignments are additive.** Repositories created outside the portal keep working exactly as
  before — nothing in grading or status tracking may start requiring an `Assignment` row.

Accepting is idempotent: the repository is looked up on GitHub before it is created, and a unique
index on `(AssignmentId, UserId)` settles the double-click race. Student repositories are private and
named `{template repository name}-{neptun}`, lower-cased like every other repository name in the model.

Students who are not organization members receive a GitHub *invitation* rather than direct access, and
invitations expire. That state is tracked per acceptance and re-sendable from `/my`; the expiry is read
from GitHub's own `expired` flag, never computed here.

## Auth model

- Cookie-based ASP.NET Identity (the API returns 401/403 rather than redirecting). We intentionally
  do **not** use `MapIdentityApi` — see the architecture plan for the rationale (no OIDC support,
  bearer-token focus, fixed shapes).
- **The auth cookies are named `ahk.auth` / `ahk.auth.external`** (`Program.ApplicationCookieName`),
  not the framework defaults. Browsers scope cookies by host and ignore the port, so on `localhost`
  every ASP.NET Identity app would otherwise share `.AspNetCore.Identity.Application`; a cookie from
  another project carrying a GUID user id crashes this int-keyed app inside `SecurityStampValidator`,
  500-ing every request including login. `OnValidatePrincipal` additionally wraps the stamp validator
  so an unreadable cookie signs the caller out rather than throwing. Do not revert either.
- `POST /api/auth/login` distinguishes its failures: the 401 body carries a `LoginFailureResponse`
  with `reason` = `InvalidCredentials` / `LockedOut` / `NotAllowed`. Sign-in uses
  `lockoutOnFailure: true`, so five wrong attempts lock the account for five minutes — without the
  distinction that is indistinguishable from a typo.
- Course context: `/api/{course}/...` routes are resolved to a `Course` and gated by the
  `CourseMember` policy. Host/admin routes live under `/api/admin/...`. Machine-to-machine webhook
  endpoints (added in the port) resolve their course from the payload/token, not the path segment.
- **Site admins can open any course.** `CourseMembershipAuthorizationHandler` grants the
  `CourseMember` policy to the `Admin` role, and `GET /api/auth/me` therefore lists *every* course for
  an admin — marked `viaSiteAdmin: true` where there is no membership record — so the SPA's course
  switcher and route guard need no special case for them.

### Impersonation ("continue as this user")

A site admin can work through another account from the users screen, to see what the person actually
sees instead of resetting their password. Two endpoints, both in `Auth/ImpersonationController.cs`:

- `POST /api/auth/impersonate/{userId}` — **site admins only**. Signs the caller in as that user with
  `SignInWithClaimsAsync`, adding the `ahk:impersonator_id` / `ahk:impersonator_name` claims.
- `POST /api/auth/impersonate/stop` — only `[Authorize]`, because the caller is now the impersonated
  user and may hold no roles at all.

The security model is the cookie. The marker lives **only** inside the data-protected application
cookie, so it cannot be forged, edited, or transplanted; `stop` reads the admin's id from there and
trusts nothing in the request. Returning **re-checks that the admin still exists and still holds
`Admin`** — if they were deleted or demoted meanwhile the session is signed out instead of restored.
Starting is refused while a marker is already present (no chaining, even when the impersonated
account is itself an admin) and refused on your own account. Everything else is an ordinary sign-in:
the session holds exactly the target's roles and memberships, so no policy anywhere is special-cased.

⚠️ `SecurityStampValidator` rebuilds the principal from the database on its validation interval and
would drop the marker, stranding the admin inside the other account. `SecurityStampValidatorOptions.
OnRefreshingPrincipal` in `Program.cs` carries it across — do not remove it.

Both transitions log at **Warning** with admin id/username, target id/username and remote IP; that is
the audit trail (there is no database table for it).
