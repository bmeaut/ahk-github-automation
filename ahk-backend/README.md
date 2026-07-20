# AHK Backend

Centralized ASP.NET Core (.NET 10) Web API for the AHK portal at `ahk.aut.bme.hu`. Replaces the
per-course Azure Functions deployments with a single site whose domain data is assigned to
**Courses**. Provides ASP.NET Identity authentication (local username/password + generic OIDC,
cookie-based) and course-scoped, membership-authorized endpoints.

> Skeleton milestone: auth + course-scoping plumbing + OpenAPI. The github-monitor /
> grade-management functionality is ported in a later milestone. See the architecture plan for the
> full design.

## Projects

- **Ahk.Web.Server** — Web API host: `Program.cs` wiring, `Auth/` (local + OIDC controllers),
  `Admin/` (host-context course management), `Courses/` (course-context endpoints),
  `CourseContext/` (course resolution middleware + membership authorization).
- **Ahk.Web.Data** — EF Core `ApplicationDbContext` (Identity + `Course`/`CourseMembership` +
  `ICourseScoped` global query filter), migrations, and the dev data seeder.
- **Ahk.Web.Server.Tests** — xUnit tests (course-scoping unit tests + API smoke tests via
  `WebApplicationFactory`).

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
- sample courses `viaubc01`, `viaubb01`

## Configuration (`AHK`-style keys under `Authentication`/`ConnectionStrings`)

- `ConnectionStrings:Default` — MSSQL connection string.
- `Authentication:Oidc:{Authority,ClientId,ClientSecret,Scopes}` — generic OIDC provider. When
  `Authority`/`ClientId` are empty, OIDC is disabled and the app runs with local login only.

## Auth model

- Cookie-based ASP.NET Identity (the API returns 401/403 rather than redirecting). We intentionally
  do **not** use `MapIdentityApi` — see the architecture plan for the rationale (no OIDC support,
  bearer-token focus, fixed shapes).
- Course context: `/api/{course}/...` routes are resolved to a `Course` and gated by the
  `CourseMember` policy. Host/admin routes live under `/api/admin/...`. Machine-to-machine webhook
  endpoints (added in the port) resolve their course from the payload/token, not the path segment.
