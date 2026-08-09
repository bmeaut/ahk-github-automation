# AHK portal — review findings

Whole-app review (ahk-backend + ahk-frontend, cross-checked against the four legacy apps and the
docs). Review only — nothing in this list has been fixed. Grouped by category, each item names the
file(s) involved. Severity is my judgment of impact, not a promise.

---

## 1. Missing / not-yet-ported functionality

These are gaps versus the legacy system's functional scope, beyond what CLAUDE.md already summarizes
as "the write-side entry points."

1. **No GitHub webhook receiver.** Nothing in `Ahk.Web.Server` maps `/api/integrations/github` (the
   path `docs/github-app.md` reserves for it). Every rule `github-monitor` enforces today — branch
   protection, single-open-PR, reviewer-must-be-assignee, comment-edit/delete tracking, the 5-run
   Actions cap — has **zero equivalent** in the portal. `CourseGitHubConfig.WorkflowRunThreshold` is
   stored and editable in the admin UI but **nothing reads it** — grep confirms no consumer. A course
   migrated to the portal today silently loses all of this enforcement.
2. **No `/ahk ok` chatops**, and therefore:
3. **No way to enter or override a grade through the portal at all**, not even manually. `IGradeService`
   (`Ahk.Web.Services/Grading/GradeService.cs`) is fully implemented — `SetGradeAsync`,
   `ConfirmAutoGradeAsync`, `RecordEvaluationResultAsync` — but grep across `Ahk.Web.Server` finds
   **no controller calling any of them**. Today's admin/instructor UI can only *read* grades
   (`GradesController`), never write one. Worth deciding whether a stopgap manual-entry admin endpoint
   is wanted before the chatops port lands, since courses may move to the portal before that ships.
4. **No HMAC-verified CI callback** (the `publish-results-pr` → grade-management webhook). CI callback
   *tokens* are fully manageable (`CoursesAdminController` CRUD, `WebhookTokenService`,
   `CiCallbackTokenHealthCheck`), but nothing accepts a signed payload and calls
   `RecordEvaluationResultAsync`. So even the automated-evaluation path has no landing point yet.
5. **Root `README.md` never mentions the portal.** It describes only the four legacy apps; a newcomer
   reading it has no idea `ahk-backend`/`ahk-frontend` exist. (See also §5.)

## 2. Correctness / code-quality findings

1. **`POST /api/auth/register` is a live, unauthenticated, unguarded endpoint** —
   [AuthController.cs:99-111](ahk-backend/Ahk.Web.Server/Auth/AuthController.cs#L99-L111). Anyone can
   create a full local account with any username/password, no admin approval, no email verification,
   no Neptun code. The SPA never calls it (`login.ts`'s own comment: "local username/password is for
   the handful of administrator-issued accounts") — grep across the frontend confirms only
   `UsersAdminController.Create` (admin-gated) is used for account creation. This directly contradicts
   the documented access model and is reachable by anyone who can reach the API. Recommend removing the
   endpoint or gating it behind `[Authorize(Roles = Admin)]`.
2. **`GitHubUsername` is existence-checked, not ownership-verified, and not unique** —
   [ProfileController.cs](ahk-backend/Ahk.Web.Server/Auth/ProfileController.cs) calls
   `GET /users/{login}` to confirm the login exists, but nothing proves the caller actually controls
   that GitHub account (no OAuth/device-flow proof), and `ApplicationUser.GitHubUsername`/
   `GitHubUserId` have no unique index (confirmed by grepping `ApplicationDbContext.cs` — only
   `MaxLength`, no `HasIndex`). Concretely: user B can claim a string that happens to be real user
   Alice's GitHub login. When B accepts an assignment, `AssignmentInviteService.AcceptAsync` calls
   `AddCollaboratorAsync(..., login: user.GitHubUsername!, ...)`, which invites/adds **Alice's real
   GitHub account** as a collaborator on B's own private homework repo — an unwanted invite sent to a
   stranger, and a spoofing vector worth deciding whether to close (verify via OAuth, or at minimum add
   a unique index so only one site account can claim a given GitHub login).
3. **TOCTOU race in repository creation** —
   [AssignmentInviteService.cs:164-172](ahk-backend/Ahk.Web.Services/Assignments/AssignmentInviteService.cs#L164-L172).
   `AcceptAsync` checks `GetRepositoryAsync` for an existing repo, then calls
   `GenerateFromTemplateAsync` if none is found. Two concurrent accept requests (double-click, two
   tabs) can both observe "not found" and both call `generate`; GitHub's second call would 422 ("name
   already exists"), which is an unhandled `GitHubOperationException` → 500 for the loser. The DB-level
   protection (unique index on `(AssignmentId, UserId)`) only catches the *second SaveChanges*, not this
   earlier GitHub-side race. Low likelihood, but worth a comment or a catch-and-retry.
4. **`SaveAssignmentRequest.TemplateRepoName` isn't validated for the `owner/name` shape** —
   [AssignmentsController.cs:177-184](ahk-backend/Ahk.Web.Server/Courses/AssignmentsController.cs#L177-L184)
   only checks non-empty. `IAssignmentService.SplitRepoName` (`AssignmentService.cs:53-61`) silently
   treats a slash-less value as `(owner: "", name: fullName)` rather than rejecting it. A typo'd
   template name (e.g. missing the org prefix) is accepted at Create time and only fails later, either
   when a student accepts (GitHub call with an empty owner) or when the instructor opts into
   `checkTemplate=true`. Consider validating the shape at Create/Update instead.
5. **No `UseExceptionHandler`/`UseHsts` in `Program.cs`** — grep of `ConfigurePipelineAsync` shows
   neither is registered for any environment. Worth confirming deliberately (e.g. relying on IIS/ANCM
   defaults in production) rather than by omission, since an unhandled exception's exact response shape
   in Production hasn't been verified either way in this review.

## 3. Test coverage gaps

1. **`ExternalAuthController`'s Neptun-matching logic has no test at all.** Grepping
   `Ahk.Web.Server.Tests` for `ExternalAuthController` finds only compiled binaries, no source
   reference. This is the controller changed this session to link eduID logins by Neptun code instead
   of email — exactly the kind of logic where a regression would silently duplicate or misattribute
   user accounts, and it's currently unverified by any automated test. `ExternalClaimsMapperTests`
   covers claim projection only, not the lookup/link/create branching in `Callback`.
2. No test exercises `CoursesAdminController.Delete`'s explicit multi-table cascade
   (`ExecuteDeleteAsync` for grade points → grades → events → submissions → acceptances → assignments)
   against a course that actually has rows in all of those tables. The logic reads correctly by
   inspection, but CLAUDE.md flags this exact area as fragile ("a new course-scoped entity whose FK is
   NoAction must be added to that list, or the delete fails") — a regression test would catch the next
   entity that's added without updating the list.

## 4. Documentation issues

1. **Dangling reference to "the architecture plan."** Both `CLAUDE.md` and `ahk-backend/README.md`
   reference "the architecture plan" for design rationale (OIDC choices, `MapIdentityApi` rejection,
   etc.), but no such file exists anywhere in the repo — confirmed by searching for the phrase across
   the whole tree. Either the document exists somewhere outside this repo and should be linked, or the
   references should point at wherever the rationale actually now lives (much of it is duplicated
   inline in CLAUDE.md already).
2. **Root `README.md` describes only the four legacy apps** and does not mention `ahk-backend`/
   `ahk-frontend` or that a migration is underway — see §1 item 5. A one-paragraph pointer to
   `ahk-backend/README.md` would close this.
3. `ahk-backend/README.md`'s "Run" section has no mention of the new `ahk-web-deploy.yaml` production
   deployment path (SSTP VPN / CIFS / Mezga) added this session — it's only in the root `CLAUDE.md`.
   Minor, but a reader of just the backend README would not find it.

## 5. Deployment / operational open items (from this session's work, not yet fully closed)

1. **Local dev databases still hold the old 5-migration history.** The `Migrations/` directory was
   reset to a single `InitialCreate` this session (for a clean production rollout). Any existing dev
   LocalDB will conflict on `dotnet ef database update` (its `__EFMigrationsHistory` references
   migration ids that no longer exist). Needs a drop/recreate before next use — already called out
   verbally, tracking here so it isn't lost.
2. **The CIFS mount fix (credentials file instead of inline `-o password=`) has not yet been confirmed
   against a real deploy run.** It was applied to fix a `mount error(13)` diagnosed from symptoms
   (likely a comma in the password truncating the inline option), but the next live workflow run is the
   first real confirmation.
3. **`VPN_CA_CERT` support was added then the user reported it unnecessary** (the endpoint's cert
   validates fine); the plan says to drop the block entirely, but worth double-checking the final
   workflow file has no leftover dead branch for it.
4. **`ahk-web-deploy.yaml` has never completed an end-to-end green run** (build → test → VPN → mount →
   mirror → web.config-driven app start → warm-up) in one pass, only fixed incrementally per failure.
   Worth one full run start-to-finish as a final confidence check before calling the pipeline done.

---

*Compiled from a manual review; no source files were modified as part of this pass.*
