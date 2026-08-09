# The CI callback

When the evaluator finishes inside a student's repository, the `publish-results-pr` GitHub Action posts the
result to the portal. This document is the contract between the two.

It matters more than most internal contracts because **the client cannot be redeployed on our schedule**: it
runs from a workflow file inside student repositories, which are created from a template and then belong to the
student. A change here breaks builds for a cohort that has already started.

The client is Go (`publish-results-pr/internal/publishtoapi`); the server is
`Ahk.Web.Server/Integrations/EvaluationResultController.cs` and
`Ahk.Web.Services/Integrations/HmacSha256Validator.cs`. The scheme was ported unchanged from
`grade-management`, and both sides' test suites carry the same golden signatures.

## Endpoint

```
POST https://ahk.aut.bme.hu/api/integrations/evaluation-result
```

⚠️ **The URL is part of the signature.** Scheme, host, path, trailing slash and any query string all have to
match what the action was configured with. Consequences worth knowing before you debug one of these:

- **It must be `https`.** An `http://` value gets a 307 from `UseHttpsRedirection`, and even if the client
  followed it, the client signed the `http` URL while the server computes the `https` one — a guaranteed
  mismatch that looks nothing like a URL problem.
- **No trailing slash**, no extra path segments.
- **Casing is safe** — both sides lower-case the URL before signing. It is the only forgiving part.
- Behind IIS, `UseForwardedHeaders` running first in the pipeline is what makes the server see the *public*
  URL rather than the internal one. Do not move it in `Program.cs`.

## Action configuration

Set these in the evaluator workflow of each **template** repository:

| Input | Value |
|---|---|
| `AHK_APPURL` | `https://ahk.aut.bme.hu/api/integrations/evaluation-result` |
| `AHK_APPTOKEN` | The token from **Site administration → Courses → *course* → CI callback tokens** |
| `AHK_APPSECRET` | That token's secret, shown alongside it |

Publishing is skipped entirely if any of the three is empty, so a template that has not been migrated yet
simply does not report — it does not fail.

Tokens are per course and can be revoked. Revoking takes effect immediately; the secret lookup is cached for an
hour, and revoking evicts the entry.

## Headers

| Header | Purpose |
|---|---|
| `X-Ahk-Token` | Identifies the course. Sent in clear, so it is an identifier, not the secret. |
| `X-Ahk-Sha256` | Base64 HMAC-SHA256 of the string below. |
| `Date` | RFC1123, e.g. `Mon, 02 Jan 2006 15:04:05 GMT`. Signed as well as checked. |
| `X-Ahk-Delivery` | Logged only. Never validated, never used for de-duplication. |

## The string to sign

Four parts, joined by single `\n` characters, with **no trailing newline**:

```
UPPERCASE(http verb)
lowercase(full url, query string included)
RFC1123 date
raw request body
```

Signed with HMAC-SHA256, base64-encoded. The key is the token's secret as **ASCII** bytes — which matches Go's
`[]byte(secret)` for the ASCII-only secrets the generator produces, and is the reason secrets must stay ASCII.

The `Date` header is checked against the server clock with a **±10 minute** window, and it is also what the
grade is timestamped with — not the moment the server happened to process it.

## Request body

```json
{
  "gitHubRepoName": "bmeaut/viaubc01-abc123",
  "gitHubBranch": "refs/pull/12/merge",
  "gitHubPullRequestNum": 12,
  "gitHubCommitHash": "aa11cc33",
  "neptunCode": "ABC123",
  "imageFiles": [],
  "result": [
    { "exerciseName": "ex1", "taskName": "t1", "points": 2, "comment": "ok" },
    { "exerciseName": "ex1", "taskName": "t2", "points": 3 }
  ],
  "origin": "https://github.com/bmeaut/viaubc01-abc123/commit/aa11cc33"
}
```

Two deliberate looseness's, neither of which should be tightened:

- **Unknown members are ignored.** `imageFiles` has no counterpart on the server and never had one. A strict
  deserializer would fail every student build at once.
- **`taskName` is not actually enforced**, despite being marked required, because .NET's validator does not
  recurse into collections. It behaved this way in `grade-management` too; enforcing it now would start
  rejecting results that pass today.

`gitHubPullRequestNum` is omitted rather than sent as `0` when there is no pull request.

### What is stored

Per-task detail is **discarded**. Points are summed per `exerciseName` (tasks with none collapse into a single
unnamed group) and ordered by name, then written as an **unconfirmed** grade record — it does not appear in the
grade listing or the CSV export until a teacher confirms it with `/ahk ok`.

## Responses

`200` with an empty body on success. Everything else is a `400` naming the problem, in this order:

| Message | Cause |
|---|---|
| `Date header missing` | No `Date` header. |
| `Date header value not valid RFC1123 string` | Malformed date. |
| `Date header value is not close enough to current date` | Runner clock is off by more than 10 minutes. |
| `X-Ahk-Sha256 header missing` | No signature. |
| `X-Ahk-Token header missing` | No token. |
| `X-Ahk-Token invalid` | Unknown token, revoked token, or a token whose course no longer exists. The three are deliberately indistinguishable. |
| `X-Ahk-Sha256 signature not valid` | Wrong secret, or — far more often — the signed URL does not match. See the endpoint section. |
| `Body cannot be deserialized as JSON: …` | Malformed body, or `gitHubRepoName`/`neptunCode` missing. |

A `500` carries the exception text, because the caller's only view of it is a build log.

⚠️ **The client treats any non-2xx as fatal and fails the student's build.** That is why an unknown repository
is not an error — the submission row is created on first sighting — and why a bad token is worth catching with
the health check before students meet it.

## Diagnosing a signature mismatch

The server logs the *URL component* of the string it signed at `Debug` level when a signature fails. Never the
body (it is student code) and never the secret. In practice the URL is the culprit almost every time; compare
it against `AHK_APPURL` character by character.
