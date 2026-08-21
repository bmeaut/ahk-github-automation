# AHK portal — pending work

Re-verified against the code on **2026-08-20**. This replaces the earlier review list: most of that
file's §1 (missing functionality) and all of its §5 (deployment) closed when the webhook receiver,
chatops and CI callback landed. What closed is recorded at the bottom, with the evidence, so it does
not get re-opened from memory.

Each item names the files involved and a rough size (S = an hour or so, M = half a day, L = more).
Severity is judgment, not a promise.

---

## P0 — housekeeping

1. **Review and merge the `admin-impersonation` branch.** Admin impersonation, the course-switcher
   reload fix, and the registration-endpoint removal are committed there but not on `master` and not
   pushed. *S.*

## P1 — security

1. **`GitHubUsername` is existence-checked, never ownership-proven, and not unique** —
   [ProfileController.cs](ahk-backend/Ahk.Web.Server/Auth/ProfileController.cs) calls
   `GET /users/{login}` to confirm the login exists, but nothing proves the caller controls that
   account, and `ApplicationDbContext` has no `HasIndex` on `GitHubUsername`/`GitHubUserId` (only
   `HasMaxLength`). User B can claim Alice's real GitHub login; on accept,
   `AssignmentInviteService.AcceptAsync` calls `AddCollaboratorAsync` with it and **Alice** receives
   a collaborator invite to B's homework repo. Minimum fix: a unique index (*S*). Real fix: OAuth /
   device-flow proof of ownership (*L*). Decide which.

## P2 — functional gaps worth a decision

1. **No way for an instructor to enter or correct a grade in the portal.** `IGradeService`
   (`Ahk.Web.Services/Grading/GradeService.cs`) is complete, but the only callers are the chatops
   handlers and `EvaluationResultController` — no admin/instructor controller writes a grade. Fixing
   a wrong grade means posting a PR comment. Legacy was the same, so this is not a regression, but
   the portal is where instructors now work. *M.*

2. **New student repositories get no branch protection**
   ([issue #96](https://github.com/BMEAUT/ahk-github-automation/issues/96)).
   [AssignmentInviteService.cs:164-190](ahk-backend/Ahk.Web.Services/Assignments/AssignmentInviteService.cs#L164-L190)
   generates the repo, enables Actions, adds the collaborator — and stops. A student can merge their
   own PR without review. The portal is now the thing creating repositories, so this is its job.
   (`BranchProtectionRuleHandler` only *reacts* to protection-rule events; it does not create one.)
   *S–M.*

3. **`ExternalAuthController` has no test at all.** Grepping `Ahk.Web.Server.Tests` finds no
   reference. This is the eduID link-by-Neptun-code logic — exactly where a regression silently
   duplicates or misattributes user accounts. `ExternalClaimsMapperTests` covers claim projection
   only, not the lookup/link/create branching in `Callback`. *M.*

4. **No `UseExceptionHandler` / `UseHsts` in the pipeline** — neither appears in
   `Program.ConfigurePipelineAsync` for any environment. Worth deciding deliberately (IIS/ANCM may
   already cover it) rather than by omission; the Production response shape for an unhandled
   exception has never been checked either way. *S.*

## P3 — correctness nits and test gaps

1. **TOCTOU race in repository creation** —
   [AssignmentInviteService.cs:164-172](ahk-backend/Ahk.Web.Services/Assignments/AssignmentInviteService.cs#L164-L172).
   `AcceptAsync` checks `GetRepositoryAsync`, then calls `GenerateFromTemplateAsync` if nothing was
   found. Two concurrent accepts (double-click, two tabs) both see "not found"; GitHub 422s the
   second, and the unhandled `GitHubOperationException` becomes a 500 for the loser. The unique index
   on `(AssignmentId, UserId)` only catches the later `SaveChanges`, not this GitHub-side race.
   *S — catch and re-read.*

2. **`TemplateRepoName` is not validated for the `owner/name` shape** —
   `AssignmentsController.Validate` only checks non-empty, and
   `IAssignmentService.SplitRepoName` silently turns a slash-less value into `(owner: "", name)`. A
   typo'd template is accepted at save time and only fails when a student accepts. *S.*

3. **No test for `CoursesAdminController.Delete`'s multi-table cascade** against a course that
   actually has grade points, grades, events, submissions, acceptances and assignments. CLAUDE.md
   flags this as fragile ("a new course-scoped entity whose FK is `NoAction` must be added to that
   list"); a regression test would catch the next entity added without updating it. *M.*

## P4 — documentation and polish

1. **Dangling reference to "the architecture plan"** — `CLAUDE.md:55` and
   `ahk-backend/README.md:187` both cite it for design rationale, and no such file exists in the
   repo. Either link where it actually lives, or point at the rationale now inlined in CLAUDE.md. *S.*

2. **`ahk-backend/README.md` never mentions the production deployment path** (`ahk-web-deploy.yaml`,
   SSTP VPN, Mezga, the hash-diffed mirror). It is documented only in the root CLAUDE.md, so a reader
   of just the backend README cannot find it. *S.*

3. **`favicon.ico` is still the Angular scaffold default** — unchanged since the `ee130df` scaffold
   commit, while the rest of the app carries the BME AUT identity. The AUT mark is 2.86:1, so this
   needs a square crop from the EPS masters in `ahk-frontend/brand/`. *S.*

4. **Students cannot see their grades in the portal.** `MyAssignmentsController` lists repositories
   only; grades reach students through PR comments, as in the legacy system. An enhancement, not a
   gap — listed so the decision is explicit. *M.*

5. **`Ahk.Web.Import` is throwaway** and should be deleted once every course has migrated. Blocked on
   the migration, not on code.

---

## GitHub issue backlog (30 open)

**The `v2`-labelled issues (#70–#91, Aug 2025) belong to an earlier rewrite effort, not this portal**
— controller refactor, .NET Aspire, DB-first, `SoftDeleteInterceptor`, wizard/role screens that do
not exist here. Recommend closing them in bulk rather than working them.

Three older `v1` bugs were **ported verbatim into the portal** and are therefore live again:

- **[#25](https://github.com/BMEAUT/ahk-github-automation/issues/25)** — `/ahk ok` does not work if
  AHK was disabled when the repository was created. The `.github/ahk-monitor.yml` gate and its
  12-hour per-repo cache were kept verbatim; CLAUDE.md calls this the most common "webhook returns
  2xx but nothing happens" cause.
- **[#1](https://github.com/BMEAUT/ahk-github-automation/issues/1)** — a *missing* `neptun.txt` is
  cached as null for the same 12 hours
  ([RepositoryEventHandlerBase.cs:107](ahk-backend/Ahk.Web.Services/GitHubWebhooks/Handlers/RepositoryEventHandlerBase.cs#L107)),
  so adding the file later takes up to half a day to take effect.
- **[#29](https://github.com/BMEAUT/ahk-github-automation/issues/29)** — the workflow-run counter
  miscounts. The port deliberately kept GitHub's own `total_count` because "a silent change here
  changes a student's grade", so the original complaint stands.

#25 and #1 share a root cause — negative results cached as long as positive ones — and would likely
be fixed together.

Also outside the portal: **[#95](https://github.com/BMEAUT/ahk-github-automation/issues/95)**
(PublishResult Markdown support) and **#90/#91** (rewritten PublishResults + its CI/CD) target
`publish-results-pr`.

---

## Closed since the previous list

Verified in the code, not assumed:

- **GitHub webhook receiver** — `Ahk.Web.Server/Integrations/GitHubWebhookController.cs` plus the
  delivery queue; all 11 legacy handlers have portal equivalents under
  `Ahk.Web.Services/GitHubWebhooks/Handlers/`. `CourseGitHubConfig.WorkflowRunThreshold` **is** read
  (`ActionWorkflowRunHandler`), contrary to the previous list.
- **`/ahk ok` chatops** — `Handlers/GradeComment/`.
- **HMAC-verified CI callback** — `Integrations/EvaluationResultController.cs`, covered by
  `EvaluationResultEndpointTests`.
- **Root `README.md`** now leads with the portal and links the per-course cutover checklist.
- **Dev migration history** — three migrations present and applying cleanly; the single-`InitialCreate`
  reset note is obsolete.
- **Deployment** — `ahk-web-deploy.yaml` has completed green end-to-end (most recently 2026-08-18,
  16m), and no `VPN_CA_CERT` branch remains in the workflow or scripts.
- **`POST /api/auth/register`** (2026-08-21) — removed outright rather than gated; self-service
  registration is not wanted. Accounts come from a BME sign-in or from an administrator.
  `ApiSmokeTests.Register_EndpointDoesNotExist` asserts both the response and that no account is
  created, so a restored endpoint fails the build.

*Compiled by reading the current source; no files were modified for this pass.*
