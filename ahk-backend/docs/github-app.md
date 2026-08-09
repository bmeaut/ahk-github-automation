# The AHK GitHub App

Every course in the portal acts on GitHub through a **GitHub App installed on that course's organization**. This
document covers what the App is for, how to register and install one, which permissions it needs and why, and
what to check when something does not work.

Read this before setting up a new course. The health dashboard in Site administration checks most of what is
described here, but it can only tell you *that* something is wrong — this explains what to do about it.

## Why an App, and why one per course

Each course points at its own GitHub organization (`Course.GitHubOrganization`), and each organization is
administered by different people. A single shared credential would give every course's staff power over every
other course's repositories, and would die the day the person who created it left the university.

A GitHub App avoids both problems: it is owned by the organization rather than a person, its permissions are
declared up front and visible to the organization's owners, and it can be uninstalled by them at any time
without touching any other course.

The App is used for two things:

| Purpose | Status |
|---|---|
| Receiving webhooks (pull requests, comments, workflow runs) and enforcing the course's rules | Live in the portal |
| Creating student repositories from an assignment's template and granting students access | Live in the portal |

One App serves both. If the organization already has an App registered for `github-monitor`, extend that one
rather than registering a second — see [Permissions](#permissions) for the grant that has to be added — and
repoint its webhook URL as described in [Cutover per course](#cutover-per-course).

## Registering the App

Do this once per organization, as an organization owner.

1. Go to the organization's **Settings → Developer settings → GitHub Apps → New GitHub App**.
2. **GitHub App name** — something identifiable, e.g. `AHK viaubc01`.
3. **Homepage URL** — `https://ahk.aut.bme.hu`.
4. **Webhook**
   - Tick **Active**.
   - **Webhook URL**: `https://ahk.aut.bme.hu/api/integrations/github`
   - **Content type**: `application/json`.
   - **Webhook secret**: generate a long random string. You will need it again in step 8.
     There is no global secret — the receiver validates `X-Hub-Signature-256` against **that course's** stored
     secret, and resolves which course a delivery belongs to from the repository name inside it.
5. **Permissions** — set exactly what the [table below](#permissions) lists. Nothing more: an App with rights
   nobody uses is a liability, and organization owners are shown this list when they install it.
6. **Subscribe to events** — the five the portal has handlers for: *Create*, *Issue comment*, *Pull request*,
   *Pull request review*, *Workflow run*.
   Subscribing to more is harmless — anything else is answered `200` with `Event X is not of interest` — but it
   makes the delivery log noisier to read.
7. **Where can this GitHub App be installed?** — *Only on this account*.
8. Create it, then on the App's page:
   - Note the **App ID**.
   - **Generate a private key**. GitHub downloads a `.pem` file — this is the only time you can get it.

## Installing it

On the App's page choose **Install App** and pick the organization.

**Repository access must be “All repositories”.**

This is not a convenience setting. Repositories created for students do not exist when the App is installed, so
under *Only select repositories* they fall outside the installation — the portal creates the repository
successfully and then gets a `404` from the very next call, trying to add the student to a repository it just
made itself. The health check reports this as a warning before it happens.

## Permissions

Set these under **Repository permissions**. Organization permissions are not needed at all.

| Permission | Level | Why |
|---|---|---|
| **Administration** | **Read & write** | Creating a repository from a template (`POST /repos/{owner}/{repo}/generate`), adding a student as a collaborator (`PUT /repos/{owner}/{repo}/collaborators/{username}`), managing their invitations, setting a repository's Actions permissions, and applying branch protection all sit behind this one. **This is the grant to add if the App already exists for `github-monitor`.** |
| **Metadata** | Read-only | Mandatory for every App. Also backs `GET /repos/{owner}/{repo}`, which the portal uses to check the template exists and is marked as a template. |
| **Contents** | Read & write | Reading the template repository, and reading `.github/ahk-monitor.yml` and `neptun.txt` out of each student repository. |
| Pull requests | Read & write | Comments, merges, assignees — the `/ahk ok` command approves and merges. |
| Issues | Read & write | Issue comments carry the `/ahk ok` chatops and every warning the rules post. |
| Actions | Read-only | Counting workflow runs against the course's threshold. |

Note that **Administration: write is broad** — it also permits changing repository settings and deleting
repositories. There is no narrower permission that covers creating repositories and adding collaborators, so
this is the floor, not a choice.

Verifying a student's GitHub username (`GET /users/{login}`) needs no permission at all. The portal sends the
installation token anyway, purely to get GitHub's authenticated rate limit of 5000 requests an hour instead of
the anonymous 60.

## Storing the credentials in the portal

**Site administration → Courses → *course* → GitHub integration**:

- **GitHub App id** — the App ID from the App's page.
- **GitHub App private key** — paste the entire contents of the `.pem` file, `-----BEGIN` line included. The
  bare base64 body that `github-monitor`'s `AHK_GitHubAppPrivateKey` holds is also accepted, so a course being
  migrated can paste what it already has.
- **Webhook secret** — the string from step 4.

Stored credentials are never sent back to the browser. An empty field means *leave the stored value alone*; use
the explicit **Clear** control to remove one. That is what makes saving an untouched form safe.

Never commit these anywhere. In production the connection string and any process-level secrets come from
environment variables; the App credentials live only in the database.

## How the portal authenticates

Each call runs as the App's *installation* on the course's organization:

1. Build a short-lived **App JWT** — RS256, ten minutes, signed with the private key, issued by the App id.
2. `GET /orgs/{org}/installation` with that JWT → the installation id for the course's organization.
3. `POST /app/installations/{id}/access_tokens` → an **installation access token**, valid for 60 minutes.
4. The token is cached per course for 50 minutes and reused.

The App JWT never leaves step 1–3; every repository operation uses the installation token. The code is
`Ahk.Web.Services/GitHub/CourseGitHubAppTokenProvider.cs`.

Note the installation is looked up **from the organization**, not from the `installation.id` a webhook delivery
carries. The course is resolved *by* organization, so the two are the same installation by construction — and
deriving a credential from a payload field would be a weaker path to the same answer. A mismatch is logged as a
warning and otherwise ignored.

## Delivery responses

What the receiver answers, and what each status means when you read it in **Advanced → Recent Deliveries**.
Only the first two are normal.

| Status | Body | Meaning |
|---|---|---|
| `200` | one line per handler | Delivered and processed. Read the lines: `no action needed` and `action not of interest` are both normal, and `event handler disabled: no ahk-monitor.yml or disabled` means the repository is not opted in. |
| `202` | `repository '…' is not mapped to a course` | The repository's organization (and repo-name prefix) match no course. Normal for repositories in the organization that are not coursework. |
| `202` | `GitHub integration is not configured` / `is turned off` | The course exists but has no GitHub integration saved, or it is switched off. **A course switched off silently ignores `/ahk ok`.** |
| `400` | `Payload signature not valid` | The secret stored on the course does not match the one on the App. |
| `400` | `X-GitHub-Event header missing` etc. | Something other than GitHub is posting, or the webhook is configured with the wrong content type. |
| `500` | `GitHub secret not configured` | The course has an organization but no webhook secret stored. Finish *Storing the credentials* below. |
| `500` | `GitHub App ID/Token not configured` | The App id or private key is missing or invalid, so no installation token could be minted. |

Benign cases answer `202` rather than an error on purpose: GitHub colours every non-2xx red, and a delivery log
full of red is a log nobody reads.

## Template repository requirements

An assignment points at a template repository in the course's organization. It must:

- be marked **Template repository** in its Settings (`is_template: true`) — without this GitHub refuses the
  generate call outright;
- contain the evaluator workflow under `.github/workflows/`, with `AHK_APPURL` pointing at the portal (see
  [ci-callback.md](ci-callback.md));
- contain `.github/ahk-monitor.yml` with `enabled: true`, or **every event from repositories generated from it
  is ignored**;
- have the branch students should start from as its **default branch** — only that branch is copied.

⚠️ The opt-in answer is cached for **12 hours** per repository. Adding `ahk-monitor.yml` to a repository that
has already been seen therefore takes up to half a day to take effect; restarting the application is the only
faster flush. This is the most common reason a correctly wired webhook appears to do nothing.

The assignment editor checks the first point when you save, and reports it as a warning rather than blocking:
an assignment may legitimately be drafted before its template exists.

Student repositories are created **private**, named `{template repository name}-{neptun}` in lower case.

## Repository invitations

When a student accepts an assignment the portal grants them `push` on their repository. GitHub then does one of
two things:

- the student is already an **organization member** → they are added outright (`204`) and can open the
  repository immediately;
- they are **not** → GitHub creates an **invitation** (`201`). Until they accept it at
  `https://github.com/{owner}/{repo}/invitations`, the repository 404s for them.

Invitations expire (7 days at the time of writing). The portal tracks the pending state per acceptance, and the
student's own page (`/my`) shows it with a **Resend invitation** button. GitHub has no way to extend an
invitation, so re-sending withdraws the stale one and issues a fresh one.

The portal never computes the expiry window itself — it reads GitHub's `expired` flag on the invitation. If
GitHub changes the policy, nothing here has to change.

## Troubleshooting

The admin health dashboard runs four checks per course. Each maps to something in this document:

| Check | What it means when it fails |
|---|---|
| **Webhook settings** | No webhook secret stored — incoming webhooks cannot be verified. Step 4 and *Storing the credentials*. |
| **GitHub access token** | The course's personal access token is invalid or cannot see the organization. Independent of the App; used only for this check today. |
| **GitHub App installation** | The App credentials do not work, the App is not installed on the organization, it lacks `administration: write`, or the installation is limited to selected repositories. Everything above. |
| **CI callback token** | No token for evaluation results to be signed with. Unrelated to the App. |

Specific failures:

- **“Installation … was not granted 'administration: write'”** — edit the App's repository permissions, then
  accept the new permissions on the organization's installation page. GitHub does **not** apply added
  permissions until an owner approves them, so changing the App alone is not enough.
- **404 from `GET /orgs/{org}/installation`** — the App is registered but not installed on that organization,
  or the course's organization name is wrong.
- **401 from the token endpoint** — wrong App id, or the private key was revoked or pasted incompletely.
  Generate a new key and store it again.
- **“limited to selected repositories”** — change the installation's repository access to *All repositories*.
- **Student says the repository 404s** — they almost certainly have an unaccepted invitation. Point them at
  `/my` on the portal.
- **Every delivery is `405` with `Allow: GET, HEAD`** — the webhook URL points at the portal but not at
  `/api/integrations/github`. The request fell through to the Angular single-page app's fallback route, which
  only answers GET. Check the path, including that it has no trailing slash.
- **Deliveries are `200` but nothing happens** — read the response body. `no ahk-monitor.yml or disabled` means
  the repository is not opted in; mind the 12-hour cache noted above.
- **No deliveries at all** — the App's installation is limited to *selected repositories*, so a repository
  created after the installation falls outside it. Or the event is not one of the five subscribed.

## Cutover per course

⚠️ **A GitHub App has exactly one webhook URL, so for a single course this is a flip, not a parallel run.**
Courses migrate independently — course A can stay on `github-monitor` while course B is on the portal — but one
course cannot be served by both at once. Doing that would need a second App registration or an
organization-level webhook.

1. Store the App id, private key, webhook secret, `WorkflowRunThreshold` and **Enabled** under
   **Site administration → Courses → *course* → GitHub integration**.
2. Check all four health checks are green on **Site administration → Health**.
3. Make sure the course has a non-revoked CI callback token (mint one, or import the legacy one — the
   `Ahk.Web.Import` tool already imports tokens).
4. Import the course's historical Cosmos data, if that has not been done.
5. **Flip** the App's webhook URL to `https://ahk.aut.bme.hu/api/integrations/github` and trim the subscribed
   events to the five listed above. Use the **Redeliver** button on a recent delivery to test without waiting
   for a student.
6. Smoke test on a scratch repository in the organization: push a branch (a branch event should appear under
   the course's dashboard), open a pull request, and comment `/ahk ok 5` as an organization member — the pull
   request should be approved and merged, and a grade should appear.
7. Update the course's **template** repositories' evaluator workflow: `AHK_APPURL`, `AHK_APPTOKEN` and
   `AHK_APPSECRET` (see [ci-callback.md](ci-callback.md)).
8. ⚠️ **Student repositories already generated keep the old workflow** and go on posting results to
   `grade-management`. Either leave that deployment running until the cohort finishes, or push a workflow
   update across the existing student repositories. Decide this *before* step 7 and record the decision.
9. Leave `github-monitor` and `grade-management` deployed until every course has migrated and the semester has
   closed.
