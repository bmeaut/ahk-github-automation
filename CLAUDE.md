# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

**Ahk** = automated homework evaluation. A toolset that automates homework submission, evaluation, and grading using GitHub, GitHub Classroom, and GitHub Actions. Concept docs: <https://akosdudas.github.io/automated-homework-evaluation/>

The repo is a monorepo of independent applications, each in its own top-level directory with its own solution/module, README, and CI workflows. There is no root-level build — each app builds separately.

It is mid-transition: the **four original apps** (`github-monitor`, `grade-management`, `review-ui`, `publish-results-pr`) are the production system today, and a **new centralized portal** (`ahk-backend` + `ahk-frontend`) is being built to replace the per-course Azure deployments with a single multi-course site at `ahk.aut.bme.hu`. The portal currently exists as a validated skeleton (auth + course-scoping + codegen); the original apps' functionality has **not** been ported into it yet. When working in this repo, be clear which of the two systems a task targets.

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

- **ahk-backend** — ASP.NET Core **.NET 10** Web API (`Ahk.Web.sln`): `Ahk.Web.Server` (API host), `Ahk.Web.Data` (EF Core + **MSSQL**, ASP.NET Identity, migrations, dev seeder), `Ahk.Web.Server.Tests` (xUnit + Moq).
- **ahk-frontend** — **Angular 21** SPA (standalone components + signals). API clients/DTOs under `src/app/api/` are **NSwag-generated** from the backend's OpenAPI doc — never hand-edit them.

Design decisions (rationale in the architecture plan and `ahk-backend/README.md`):
- **Auth**: cookie-based ASP.NET Identity (returns 401/403, never redirects) + a **generic OIDC** external provider (config `Authentication:Oidc:*`; disabled when empty). Deliberately **not** `MapIdentityApi` (no OIDC support, bearer-token focus, fixed shapes).
- **Course scoping**: path segment `/api/{course}/...`; `CourseResolutionMiddleware` resolves the `Course` and an `ICurrentCourseProvider` drives an EF Core global query filter over `ICourseScoped` entities; the `CourseMember` authorization policy gates access. Host/admin routes live under `/api/admin/...`. Machine-to-machine endpoints (webhooks / CI callbacks, added during the port) will resolve their course from the payload/token, **not** the path segment.
- **Dev**: both run HTTPS. The Angular dev proxy (`proxy.conf.js`) forwards `/api/*` to the backend so calls are **same-origin — no CORS**, and `API_BASE_URL` is provided as `''` so generated clients issue relative requests. Press F5 with the **"Full stack (backend + frontend)"** compound in `.vscode/launch.json` to start both.
- `CourseProbe` controller + `CourseNote` entity are throwaway scoping probes, removed when real course endpoints land in the port.

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
Needs MSSQL (LocalDB by default; `ConnectionStrings:Default` in `appsettings.Development.json`). The dev seeder (Development env only) creates `admin`/`Admin123!` (site admin), `instructor`/`Instructor123!` (member of `viaubc01` only), and sample courses `viaubc01`, `viaubb01`.

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

### CI
`.github/workflows/*-build.yaml` build+test each app (path-filtered so only the changed app runs). `*-azure-publish.yaml` / `*-docker-publish.yaml` deploy. Several azure-publish workflows are per-instance (e.g. `VIAUBC01`, `viaubb01`) — deployments are duplicated per course/organization instance.

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
