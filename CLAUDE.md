# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

**Ahk** = automated homework evaluation. A toolset that automates homework submission, evaluation, and grading using GitHub, GitHub Classroom, and GitHub Actions. Concept docs: <https://akosdudas.github.io/automated-homework-evaluation/>

The repo is a monorepo of independent applications, each in its own top-level directory with its own solution/module, README, and CI workflows. There is no root-level build — each app builds separately.

It is mid-transition: the **four original apps** (`github-monitor`, `grade-management`, `review-ui`, `publish-results-pr`) are the production system today, and a **new centralized portal** (`ahk-backend` + `ahk-frontend`) is being built to replace the per-course Azure deployments with a single multi-course site at `ahk.aut.bme.hu`. The portal now has auth (local + BME OIDC), the full course-scoped **data model**, the domain **services**, the read endpoints (`grades`, `statuses`, CSV export), the **site administration surface** (course CRUD, per-course GitHub integration, CI callback tokens, staff, users/roles, health checks) with its Angular console, and a one-time Cosmos→MSSQL importer. The write-side entry points are now ported too — the **GitHub webhook receiver**, **`/ahk ok` chatops** and the **HMAC-verified CI callback** — so the portal is feature-complete and a course can run on it with no Azure Functions. The four apps stay deployed for courses that have not migrated; **a GitHub App has one webhook URL, so per course the switch is a flip, not a parallel run** (cutover checklist in `ahk-backend/docs/github-app.md`). When working in this repo, be clear which of the two systems a task targets.

## The four applications and how they connect

```
Student pushes / opens PR
        │
        ▼
GitHub org (managed by GitHub Classroom)
        │ webhooks                    GitHub Actions runs evaluator container
        ▼                                     │ produces result.txt + images
[github-monitor]  (.NET Azure Function)       ▼
  - enforces workflow rules on repos    [publish-results-pr]  (Go container)
  - /ahk ok chatops approval/grading      - formats results as PR comment
        │ Azure Queue Storage messages    - POSTs results to grade-management
        │                                         │ HMAC-signed HTTP
        ▼                                         ▼
[grade-management]  (.NET Azure Function) ◄───────┘
  - queue-triggered: grade + status events → CosmosDB
  - http: evaluation-result webhook, list-grades, list-statuses
        │ HTTP API (Function master key)
        ▼
[review-ui]  (Blazor WebAssembly, browser-only)
  - teacher dashboard of statuses + grades
```

Key integration contracts (keep these in sync when changing either side):
- **github-monitor → grade-management**: Azure Queue Storage. Queue names are hard-coded in the `[QueueTrigger(...)]` attributes in `grade-management/.../Functions/**` (e.g. `ahksetgrade`, `ahkconfirmautograde`, `ahkstatustracking*`) and must match what github-monitor's `Services/GradeStore` and `Services/StatusTrackingStore` write. Both sides share duplicated DTO shapes (`Services/.../Dto/*.cs` vs `Functions/.../Dto/*.cs`).
- **publish-results-pr → grade-management**: HMAC-SHA256 signed HTTP to the `evaluation-result` webhook. The signing scheme (verb\nurl\ndate\npayload, base64 HMAC, `X-Ahk-Token`/`X-Ahk-Sha256`/`Date` headers, 10-min skew) is implemented in Go (`internal/publishtoapi`) and validated in .NET (`grade-management/.../Helpers/HmacSha256Validator.cs`). Changing one requires changing the other. See `grade-management/README.md` for the full spec.
- **github-monitor** authenticates to GitHub as a GitHub App per-installation (`Services/GitHubClientFactory`), using Octokit.

## The new centralized portal (ahk-backend + ahk-frontend)

Replaces the per-course Azure Function deployments with one site whose domain data is assigned to **Courses** (each course = what used to be a separate deployment, e.g. `viaubc01`). This is **not** multitenant isolation — it's course-scoped data with membership-based authorization.

- **ahk-backend** — ASP.NET Core **.NET 10** (`Ahk.Web.slnx`). Layering: `Ahk.Web.Server` (controllers/middleware/auth, thin) → `Ahk.Web.Services` (domain logic) → `Ahk.Web.Data` (EF Core + **MSSQL**, Identity, migrations, seeder, `Normalize`). `Ahk.Web.Import` is a **throwaway** Cosmos-JSON→MSSQL console importer that references Data directly and **bypasses Services** (bulk movement, not domain ops). Tests in `Ahk.Web.Server.Tests` (xUnit + Moq).
- **No repository layer** — `DbContext` is already Unit of Work + repository. The legacy Cosmos repositories exist only because Cosmos has no composable LINQ context; that reason is gone.
- **ahk-frontend** — **Angular 21** SPA (standalone components + signals). API clients/DTOs under `src/app/api/` are **NSwag-generated** from the backend's OpenAPI doc — never hand-edit them.
- **Octokit is the portal's single GitHub API client**, built by `ICourseGitHubClientFactory` (`Ahk.Web.Services/GitHub/`). One exception: `CourseGitHubAppTokenProvider` stays on the named `"github"` `HttpClient` because it is the auth bootstrap (hand-rolled RS256 App JWT → installation token), not an API call, and moving it would change the permissions shape `GitHubAppInstallationHealthCheck` reads. `IGitHubRepositoryService`'s interface and its four record projections are deliberately Octokit-free — that is what lets `AssignmentInviteTests` mock it strictly.

Design decisions (rationale in the architecture plan and `ahk-backend/README.md`):
- **Auth**: cookie-based ASP.NET Identity (returns 401/403, never redirects) + an **OIDC** external provider (config `Authentication:Oidc:*`; disabled when empty). Deliberately **not** `MapIdentityApi` (no OIDC support, bearer-token focus, fixed shapes).
- **OIDC provider is BME's Shibboleth IdP** (`https://idp.bme.hu`, client `AUTAhkClient`). Non-obvious constraints, all encoded in `OidcOptions` defaults — do not "fix" them back:
  - Request **exactly** the registered scopes (`openid email userinfo`). `profile` is **not** registered; asking for it gets the request rejected.
  - **PKCE off** — the IdP does not advertise `code_challenge_methods_supported`. We are a confidential client (`client_secret_post`), so PKCE is defence-in-depth only.
  - **`ResponseMode = query`** — ASP.NET's `form_post` default makes the callback a cross-site POST, which drops the correlation cookie under `SameSite=Lax` ("Correlation failed").
  - **No `end_session_endpoint`** is advertised, so logout is local-only; `POST /api/auth/logout` returns `{endSessionUrl}` (null today) and the SPA navigates there if set.
  - `OpenIdConnectOptions` ships **only `DeleteClaim` actions** — claims arriving from the *userinfo* endpoint (BME sends `email`, `name`, `neptun_code`, `eduperson_scoped_affiliation` there) are silently dropped unless explicitly mapped in `Program.cs`.
  - Persisted onto `ApplicationUser`: `NeptunCode` and `Affiliation` (multi-valued, joined with `;`), re-synced on every login by `Auth/ExternalClaimsMapper.cs`.
  - **eduID identifies a user by Neptun code, not email/username.** `ApplicationUser.NeptunCode` is filtered-unique (blank stored as null, may repeat; any value unique). First OIDC login matches an existing account by Neptun and links the external login, so an admin-pre-created account is never duplicated (`ExternalAuthController`). Admin create/update enforce the same rule (blank→null, else `Normalize.Neptun`, 400 on clash). eduID **always refreshes email**; the claim sync never writes username.
  - The client secret is never in config — `dotnet user-secrets` locally, `Authentication__Oidc__ClientSecret` in production.
- **Dev mock OIDC provider** (`Ahk.Web.Server/MockOidc/`): only the production redirect URI is registered with BME, so localhost cannot use the real IdP. Enabled by `Authentication:Oidc:UseMockProvider` in Development; serves discovery/authorize/token/userinfo/JWKS at `/mock-oidc` and issues **genuinely signed** RS256 id_tokens so the real validation path runs. Switch persona with `GET /mock-oidc/persona?user=instructor|student|noclaims`.
- **Course scoping**: path segment `/api/{course}/...`; `CourseResolutionMiddleware` resolves the `Course` and an `ICurrentCourseProvider` drives an EF Core global query filter over `ICourseScoped` entities; the `CourseMember` authorization policy gates access. Host/admin routes live under `/api/admin/...`. Machine-to-machine endpoints have **no** `{course}` segment and resolve their course from the payload/token instead: `POST /api/integrations/github` (from `repository.full_name`, via `ICourseResolutionService.ResolveByRepositoryAsync`) and `POST /api/integrations/evaluation-result` (from the `X-Ahk-Token` header). Both are `[AllowAnonymous]` — a signature is the authentication, and `Program.cs` deliberately has no `FallbackPolicy` — and both are `[ApiExplorerSettings(IgnoreApi = true)]` so NSwag emits no client for them.
- **Site admins can open any course.** `CourseMembershipAuthorizationHandler` grants `CourseMember` to the `Admin` role, so `GET /api/auth/me` lists *every* course for an admin (flagged `viaSiteAdmin` where there is no membership row). The SPA's switcher and `courseGuard` read that one list and need no admin special case — keep it that way rather than branching on `isAdmin()` in new screens.
- **Admin API credential rule**: stored secrets are never returned (only `has*` flags plus a last-four hint). On update, each credential field means: `null` = leave alone, `""` = clear, anything else = replace. That is what makes saving an untouched form safe.
- **Course health checks** (`Ahk.Web.Services/Health/`): each is an `ICourseHealthCheck` registered in DI and discovered by `CourseHealthService`, so adding one is a class plus a registration line — controller and UI unchanged. Four today: webhook settings (local), GitHub access token (real call to api.github.com, 10s timeout), GitHub App installation, CI callback token. Checks must not throw; return a `Failed` result instead, or one unreachable course takes the whole dashboard down. A course's status is the worst of its checks.
- **Dev**: both run HTTPS. The Angular dev proxy (`proxy.conf.js`) forwards `/api/*` to the backend so calls are **same-origin — no CORS**, and `API_BASE_URL` is provided as `''` so generated clients issue relative requests. Press F5 with the **"Full stack (backend + frontend)"** compound in `.vscode/launch.json` to start both.
- **Domain model** (`Ahk.Web.Data/Entities`): `Course` 1:1 `CourseGitHubConfig` (per-course GitHub App creds + `WorkflowRunThreshold` — was per-deployment `AHK_*` — plus `GitHubAccessToken`, a PAT used for REST calls that need no installation token, today only the health check); `Student`/`Submission` replace the neptun/repo-name strings; `SubmissionEvent` (TPH: Repository/Branch/PullRequest/WorkflowRun) and `GradeRecord`+`GradeExercisePoint` are **append-only** — current state is projected, never updated. `CourseWebhookToken.Token` is globally unique because the CI callback carries no `{course}` segment; its `Secret` is a plaintext column (HMAC needs the raw key) and the admin API **returns it** (list + detail) so the console can re-copy it later — a deliberate exception to the never-return rule, justified because any admin can mint an equivalent token anyway.
- **Assignments** (`Ahk.Web.Data/Entities/Assignment.cs`): student repos are named `{RepoNamePrefix}-{neptun}`, falling back to the template repo's bare name when `RepoNamePrefix` is blank (keeps pre-prefix assignments working) — logic in `AssignmentInviteService.BuildRepositoryName`. The template repo is validated **advisorily** (`AssignmentService.CheckTemplateAsync` → `POST api/{course}/assignments/check-template`): it reports existence + `is_template` as a warning and **never blocks saving** (an assignment may be drafted before its template exists). A bad template otherwise only fails at student-accept time as a 502.
- **Webhook receiver** (`Ahk.Web.Services/GitHubWebhooks/`): `GitHubWebhookDispatcher` selects handlers by `IGitHubWebhookHandler.GitHubEventName`; per-delivery state (course id, delivery id, body, `IGitHubClient`, run threshold) travels on `GitHubWebhookContext`, so handlers are stateless scoped DI registrations. **DI registration order is dispatch order** — that is what replaced github-monitor's explicit config builder, and handlers post comments whose order is visible to students. The `.github/ahk-monitor.yml` `enabled: true` gate and its **12-hour** per-repo-id cache are kept verbatim: adding the file to an already-seen repo takes up to half a day to take effect, and an app restart is the only faster flush. This is the most common cause of "webhook delivers 200 but nothing happens" — read the `WebhookResult` body, it says `no ahk-monitor.yml or disabled`.
- ⚠️ **At most one handler per GitHub event may write a `SubmissionEvent`.** `GitHubDeliveryId` is globally unique (the redelivery guard) but one delivery fans out to several handlers, so a second writer's rows are silently swallowed by the guard, or rejected by the unique index on SQL Server. Handlers that write are marked `IStatusEventWriter` and `WebhookHandlerRegistrationTests` fails the build if two share an event.
- ⚠️ **Course resolution necessarily precedes signature validation** in the webhook receiver: the secret is per course (`CourseGitHubConfig.GitHubWebhookSecret`) and the only thing identifying the course is `repository.full_name` *inside* the body. Everything before the HMAC check is deliberately inert — one property read from a `JsonDocument` that is then disposed, two indexed reads, no writes/GitHub calls/body logging — and every signature failure returns the same 400. Don't add work to that stretch. Benign cases (repo in no course, integration off) answer **202, not 4xx**, because GitHub colours non-2xx red in the delivery log.
- ⚠️ **`AHK_APPURL` must byte-match the CI callback's public URL** (`https://ahk.aut.bme.hu/api/integrations/evaluation-result`): the Go client signs the URL, so scheme, host, path and trailing slash all matter (casing does not — both sides lower it). `http://` fails confusingly, via a 307 and then a scheme mismatch. `UseForwardedHeaders` must stay first in the pipeline for `GetDisplayUrl()` to yield the public URL behind IIS. Full contract in `ahk-backend/docs/ci-callback.md`.
- ⚠️ **`ICourseGitHubAppTokenProvider.GetForCourseAsync(Course)` returns null unless the course was loaded with `Include(c => c.GitHubConfig)`** — it reads `course.GitHubConfig?.GitHubAppId`. `ResolveByRepositoryAsync` does *not* include it, so m2m paths must use the `(int courseId, …)` overload or they silently degrade to "course not connected to GitHub".
- ⚠️ **The course query filter matches nothing when no course is resolved.** `ICurrentCourseProvider` is set by whichever entry point resolved the course. Anything without HTTP course context — dev seeder, importer, services — must use `IgnoreQueryFilters()` and filter on `courseId` itself, or it silently reads **zero rows**. Service methods therefore always take an explicit `int courseId` and never read the provider.

## Build, test, run

The four original apps target **.NET 6** (Azure Functions v4) except publish-results-pr which is **Go 1.17**. The new portal targets **.NET 10** (ahk-backend) and **Angular 21 / Node** (ahk-frontend).

### github-monitor / grade-management / review-ui (.NET)
```bash
# from the app directory (github-monitor, grade-management, or review-ui)
dotnet build
dotnet test                                    # run all tests
dotnet test --filter "FullyQualifiedName~HmacSha256ValidatorTest"   # single test class
dotnet test --filter "Name=SomeTestMethod"     # single test method
```
- github-monitor and grade-management are Azure Functions — run locally with `func start` (Azure Functions Core Tools) from the function project directory. They need `local.settings.json` (gitignored) supplying the `AHK_*` env vars.
- review-ui: `dotnet run` (or `dotnet watch`) from `review-ui/Ahk.Review.Ui`. It is a standalone WASM app; configure the backend URL in `wwwroot/appsettings.json`.
- Tests use **xUnit + Moq**; handler tests mock the GitHub client, memory cache, and stores (see `Tests/.../Helpers/*MockFactory.cs`).

### publish-results-pr (Go)
```bash
# from publish-results-pr
go build
go test ./... -test.v
```
Ships as a container (`Dockerfile`) published to `ghcr.io/akosdudas/ahk-publish-results-pr`, invoked as a GitHub Action step. `.devcontainer` is provided for development.

### ahk-backend (.NET 10)
```bash
# from ahk-backend
dotnet build
dotnet test                                                # xUnit
dotnet run --project Ahk.Web.Server --launch-profile https # https://localhost:7443, Swagger at /swagger

# EF Core migrations use the design-time factory in Ahk.Web.Data (both --project and --startup-project point there)
dotnet ef database update --project Ahk.Web.Data --startup-project Ahk.Web.Data
dotnet ef migrations add <Name> --project Ahk.Web.Data --startup-project Ahk.Web.Data --output-dir Migrations
```
Needs MSSQL (LocalDB by default; `ConnectionStrings:Default` in `appsettings.Development.json`). The dev seeder (Development env only) creates `admin`/`Admin123!` (site admin), `instructor`/`Instructor123!` (member of `viaubc01` only), courses `viaubc01`/`viaubb01` with GitHub config + CI token, and sample students/submissions/events/grades. The two courses are deliberately configured differently so the admin health dashboard shows a mix of states.

⚠️ **The seeder is create-if-missing.** Editing seeded values changes nothing on a dev database that already has those rows — delete the course (or the database) to see the new values. It also calls `db.Database.MigrateAsync()` on startup, so running the backend in Development **auto-applies pending migrations** to the dev DB (prod still applies the `migrate.sql` artifact by hand).

```bash
# one-time Cosmos-export import (throwaway tool; delete once every course is migrated)
dotnet run --project Ahk.Web.Import -- --course <slug> --connection "<mssql>" \
  --grades grades.json --events events.json --tokens tokens.json   # --repo-prefix, --force
```

### ahk-frontend (Angular 21)
```bash
# from ahk-frontend
npm install
npm start              # ng serve --ssl on https://localhost:4200, proxy → https://localhost:7443
npm run build
npm test               # vitest
npm run generate-api   # regenerate src/app/api from the backend's OpenAPI (backend must be running)
```
`nswag.json` pins `"runtime": "Net100"` (the default Net90 binary needs .NET 9, which is not installed here).

### Portal gotchas
- **A running server locks its binary** — `dotnet build` fails with MSB3027; stop it first. `pkill -f` is unreliable here; free ports with PowerShell `Get-NetTCPConnection -LocalPort 7443 -State Listen | ... Stop-Process -Force`.
- **Never hand-delete a migration file** — the model snapshot stays advanced and the next `migrations add` scaffolds an empty diff. Use `dotnet ef migrations remove`, or delete the whole `Migrations/` dir and regenerate.
- **SQL Server rejects multiple cascade paths**: `Course`→`Student`→`Submission` alongside `Course`→`Submission` forces the `Student` FKs to `DeleteBehavior.NoAction`. Consequence: **deleting a course cannot rely on the database alone.** `CoursesAdminController.Delete` removes grade points, grades, events and submissions explicitly with `ExecuteDeleteAsync` before the course row goes — a new course-scoped entity whose FK is `NoAction` must be added to that list, or the delete fails on a foreign-key violation.
- **`sqlcmd` needs `SET QUOTED_IDENTIFIER ON`** before DML on `AspNetUsers`/`SubmissionEvents` — the filtered unique index on `GitHubDeliveryId` makes the default fail.
- **Auth cookies are named `ahk.auth` / `ahk.auth.external`, not the framework defaults** (`Program.ApplicationCookieName`). Browsers scope cookies by host and **ignore the port**, so on `localhost` every ASP.NET Identity app shares `.AspNetCore.Identity.Application`. A cookie from another project whose user id is a GUID reaches this int-keyed app and throws `"… is not a valid value for Int32"` inside `SecurityStampValidator` — a 500 on *every* request, including login. `OnValidatePrincipal` also wraps the stamp validator so an unreadable cookie signs the caller out instead of throwing. Do not revert either to the defaults.
- **Windows PowerShell 5.1 cannot load .NET 10 assemblies** (`Add-Type` throws); to find a type's namespace, `grep -ao` the DLL instead.
- **No Docker in this environment** — dev dependencies are built in-app (e.g. the mock OIDC provider), not containerized.
- **Verify UI changes by screenshotting the running app**, not by trusting the build. Chrome is at `/c/Program Files/Google/Chrome/Application/chrome.exe`. A plain `--headless=new --screenshot` fires before Angular hydrates and yields a near-empty page — add `--virtual-time-budget=4000`. For screens behind login, start Chrome with `--remote-debugging-port=9222` and drive the DevTools protocol from a Node script: Node 24 ships a built-in `WebSocket`, so this needs no npm dependency. Always pass `--ignore-certificate-errors` (self-signed dev cert). When reading a value back from `Runtime.evaluate`, the RemoteObject is doubly nested at `msg.result.result.value` — reading `msg.result.value` silently yields `undefined` (the clicks still fire, so screenshots look fine while probes read blank).
- **No image tooling** — no PIL, no ImageMagick. To read a PNG's pixels (sampling a brand colour, checking dimensions) decode it by hand with `zlib` + `struct`. EPS files are text-ish: `%%CMYKCustomColor` in the header carries the print colour spec.
- **Prod hosting is same-origin**: the backend serves the Angular SPA — `UseDefaultFiles`/`UseStaticFiles` + `MapFallbackToFile("index.html")` in `Program.cs`, `ng build` output copied into `wwwroot`. Only dev uses the proxy.
- **`web.config` is checked in with its SDK transform disabled** (`IsTransformWebConfigDisabled=true`): Mezga registers ANCM under the **V1** name `AspNetCoreModule`, so the SDK-generated V2 web.config fails to start the app there. Don't delete the file or re-enable the transform.
- **`SelfContained=true` needs a `RuntimeIdentifier`** — pinned to `win-x64` in `Mezga.pubxml`; the server has no .NET runtime.

### CI
`.github/workflows/*-build.yaml` build+test each app (path-filtered so only the changed app runs). `*-azure-publish.yaml` / `*-docker-publish.yaml` deploy. Several azure-publish workflows are per-instance (e.g. `VIAUBC01`, `viaubb01`) — deployments are duplicated per course/organization instance.
The portal has its own `ahk-web-deploy.yaml` (manual `workflow_dispatch`): publishes self-contained `win-x64` via the `Mezga.pubxml` profile, tunnels to the on-prem IIS server **Mezga** over **SSTP VPN**, mirrors the build over a CIFS share (`rsync`, `app_offline.htm` bracketing). Migrations are **not** auto-applied — CI emits an idempotent `migrate.sql` artifact applied by hand to a fresh DB.

## github-monitor architecture (the most complex app)

- **Entry point**: `GitHubMonitorFunction.cs` — single anonymous HTTP webhook. It validates the `X-Hub-Signature-256` HMAC against `AHK_GitHubWebhookSecret` before doing anything, then hands the raw body to `EventDispatchService`.
- **Dispatch**: `Services/EventDispatch/EventDispatchService` maps a GitHub event name → list of handler types (registered in `Startup.cs` via `EventDispatchConfigBuilder`). Handlers run independently; one throwing is caught and logged, others still run.
- **Handlers**: `EventHandlers/**`. Most extend `RepositoryEventBase<TPayload>`, which:
  - deserializes the Octokit payload,
  - creates a per-installation `GitHubClient`,
  - **short-circuits unless the repo has `.github/ahk-monitor.yml` with `enabled: true`** (cached 12h). New handlers should derive from this base to inherit the enablement gate and the `neptun.txt` / org-membership caching helpers.
- **Two handler families** run side-by-side: rule-enforcement handlers (branch protection, duplicate PR, review→assignee, comment edit/delete, workflow-run limit) and `StatusTracking/**` + `GradeComment/**` handlers that emit events to the queues for grade-management.
- **Store abstraction**: `IGradeStore` / `IStatusTrackingStore` have `*AzureQueue` and `*Noop` implementations. `Startup.cs` wires the Noop variants when `AHK_EventsQueueConnectionString` is absent, so the app runs fully without grade-management.

## Conventions

- **Config**: all runtime config comes from `AHK_`-prefixed environment variables (bound via `AddEnvironmentVariables("AHK_")` in github-monitor; direct `AHK_*` names elsewhere). See each app's README for the exact variables. Never commit secrets; `local.settings.json` is gitignored.
- **Enabling a repo for github-monitor**: the repo needs `.github/ahk-monitor.yml` containing `enabled: true` on its default branch, otherwise all its events are ignored.
- **Teacher grading chatops**: `/ahk ok`, `/ahk ok 5`, `/ahk ok 5 3.5 0` in a PR comment approves/merges and records grades (numbers map positionally to exercises). Parsing lives in `Helpers/GradeCommentParser.cs`.
- **Style is enforced at build time**: StyleCop.Analyzers + `EnforceCodeStyleInBuild` + `AnalysisMode=AllEnabledByDefault`, and grade-management/review-ui set `TreatWarningsAsErrors=true`. A large root `.editorconfig` defines the rules — match existing style exactly or the build fails. Match idiom per project (e.g. github-monitor uses explicit namespaces; review-ui uses `ImplicitUsings`/`Nullable` enabled).
- **result.txt evaluation format** (produced by evaluators, parsed by publish-results-pr): lines of `###ahk#taskname#result#comment`, with optional `group@` prefix on taskname for grouped totals. Full spec in `publish-results-pr/README.md`.
- **Portal conventions differ from the original apps**: ahk-backend uses standard ASP.NET config (`appsettings*.json` — `ConnectionStrings:Default`, `Authentication:Oidc:*`), **not** `AHK_` env vars, and uses the default .NET 10 SDK analyzers (no `TreatWarningsAsErrors`), so it is not bound by the root `.editorconfig`'s StyleCop rules. ahk-frontend follows the Angular style (2-space, standalone components + signals).
- **Portal UI**: the design system lives in `ahk-frontend/src/styles.scss` — tokens plus the shared classes (`page`, `card`, `field`, `btn`, `table.data`, `badge`, `notice`, `dot`). Component stylesheets are Angular-scoped, so compose from those classes instead of restyling buttons/tables per screen. Enums cross the wire as **names** (`JsonStringEnumConverter` in `Program.cs`), which is why NSwag emits string-literal unions like `CourseRole = 'Instructor' | 'Admin'`.
- **Portal look is the BME AUT identity**, derived from aut.bme.hu — crimson Georgia headings, Verdana body, parchment (`#dbd9c0`) table headers, the department logo. Type does three jobs: `--font-display` (Georgia) for headings, `--font-ui` (Verdana) for labels/controls, `--font-mono` for machine identifiers (slugs, orgs, repos, Neptun codes, tokens) — do not "unify" them. `--brand` (`#a4001e`, headings + primary actions), `--link` (`#074371` navy, navigation) and `--bad` (`#801b1b`, broken) are **three deliberately different reds**; collapsing them loses meaning. The logo masters and every colour's provenance live in `ahk-frontend/brand/` (BME AUT identity pack + the eduID login logo); the web copies the app loads are in `ahk-frontend/public/`. No square favicon exists yet (the mark is 2.86:1); a follow-up needs a square crop from the EPS. The **login screen leads with eduID** (the federated `/api/auth/external/challenge` flow); local username/password is collapsed behind an "I don't have an eduID account" link. The eduID button follows the [eduID brand](https://eduid.hu/hu/depo/) but renders its own English "Login" rather than the official Hungarian-label PNG; `--eduid` blue is scoped to that button, never in the global palette.
- **Portal code style**: **no top-level statements** (`Program` is an explicit class with `Main`); **`int` keys everywhere**, including Identity (`IdentityUser<int>`); the domain term is **Course**, never "tenant".
- **Frontend API errors**: use `readApiError(err, fallback)` from `ahk-frontend/src/app/core/api-error.ts` instead of parsing a `SwaggerException` inline. It pulls out the API's own `{error}`/`{errors}` message and reports status 0 as "the server is not responding" — without that, a generated-client failure surfaces as a misleading domain error (a stopped backend once looked exactly like "wrong password").
- **Portal tests**: course-scoping is tested against `ApplicationDbContext` directly with EF InMemory + a mutable `ICurrentCourseProvider` double. `WebApplicationFactory` DbContext swaps must remove **both** `DbContextOptions<T>` and EF 9+'s `IDbContextOptionsConfiguration<T>` descriptors, else two providers register. `Ahk.Web.Services` exposes internals via `InternalsVisibleTo`. Frontend single run: `npx ng test --watch=false`. Controller tests can run over a real `UserManager<ApplicationUser>` on EF InMemory (see `UserNeptunTests`) — but InMemory does **not** enforce filtered unique indexes, so uniqueness is proven via the controller's pre-check, not the DB.
- **Legacy parity is a hard constraint when porting**: grade semantics (append-only, positional `ex0`/`ex1` name carry-forward, per-exercise summing) and the CSV layout are covered by parity tests — changing them changes existing courses' grades. One deliberate deviation: `CsvExporter` sorts columns `Ordinal` rather than culture-sensitively.
