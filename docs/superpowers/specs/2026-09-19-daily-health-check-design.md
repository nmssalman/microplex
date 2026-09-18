# Daily Automated Health-Check — Design Spec

Date: 2026-09-19
Status: Approved for implementation

## Problem

Microplex (https://microplex.lk) has no automated monitoring of its public
site or its SMS/Email gateway APIs. Regressions in production (a broken
page, a dead API key path, the gateway rejecting requests, credits running
out) are currently only discovered when a customer complains. We want a
fully automated daily check that exercises the real production system
end-to-end and emails a readable pass/fail report every morning.

## Goals

- Run once a day at ~4:00 AM Asia/Colombo time, unattended, indefinitely
  (not tied to any developer's machine being on).
- Exercise, against production:
  1. The public site pages (Home, About, Solutions, Contact, Privacy).
  2. The "Solution integration" inquiry-email flow (the Solutions page
     integration modal → `/Inquiry/Submit`).
  3. SMS Send API (`POST /api/sms/send`).
  4. SMS Balance API (`GET /api/sms/balance`).
  5. Email Send API (`POST /api/email/send`).
  6. Email Balance API (`GET /api/email/balance`).
  7. Other baseline features: login page reachability, auth-gate
     correctness on admin routes, and graceful handling of unknown paths.
- Produce one self-contained HTML report, color-coded PASS (green) /
  WARNING (orange) / FAIL (red) per check, and email it to
  `contacts.nmssalman@gmail.com`.
- Never let one failing check abort the rest of the run.

## Non-goals

- Load/performance testing beyond a simple slow-response warning.
- Testing authenticated admin flows (Clients CRUD, Dashboard content,
  Insights) beyond confirming they are correctly gated behind login.
- Historical trend storage/dashboards (out of scope for v1 — each run is
  a self-contained email).
- Modifying application code under test; this is an external, read-mostly
  black-box check (it does perform real sends against the live SMS/Email
  gateways, by design, since that's the only way to verify they work).

## Decisions made during brainstorming

- **Execution platform: GitHub Actions cron workflow**, not a Claude
  Code scheduled task. Claude Code's own scheduler only fires while the
  desktop app is open on a specific machine — unsuitable for a guaranteed
  4 AM run. GitHub Actions runs on GitHub's infrastructure independent of
  any local machine.
- **Script language: Python 3**, run directly on the `ubuntu-latest`
  runner (preinstalled, no project/solution wiring needed). This is a
  synthetic-monitoring script, not application logic, so it does not need
  to live in the .NET solution.
- **Test credentials: a new, dedicated QA test `Client`** record, created
  by the user via the existing admin UI (`/Clients/Create` then
  `/ApiDocumentation/Sms` and `/ApiDocumentation/Email` to generate keys).
  Using a real client with its own API keys and credits means the checks
  test the exact same code path a real customer uses, without touching
  any real customer's account or credits.
- **Report delivery: Brevo transactional email API**, the same provider
  Microplex's own `EmailSender` already uses in production, called
  directly from the workflow rather than through Microplex's own
  `/api/email/send`. This decouples report delivery from the very system
  being tested — if the Email Send API is broken, the report about that
  breakage must still arrive.
- **Timezone: Asia/Colombo (UTC+5:30)**, matching the business and the
  app's own `SriLankaTime` utility.
- **Schedule time offset**: `35 22 * * *` UTC (≈ 04:05 Colombo), not an
  exact top-of-hour cron, per general GitHub Actions best practice
  (avoids the congestion around `:00`/`:30` marks).

## Architecture

```
.github/workflows/daily-health-check.yml   # cron trigger + workflow_dispatch
tools/health-check/run_check.py            # the check + report + send script
tools/health-check/requirements.txt        # just "requests"
```

The workflow:
1. Checks out the repo (only needed to run the script; the script itself
   never touches this repo's build or the local checkout beyond reading
   its own file).
2. Sets up Python, installs `requests`.
3. Runs `run_check.py`, passing secrets as environment variables.
4. The script exits non-zero if the report itself could not be built or
   sent (a hard infra failure), but exits zero even when individual
   checks FAIL — those failures are communicated via the emailed report,
   not via workflow failure status. (A future enhancement could also
   fail the workflow run on FAIL-level results for GitHub's own
   notification/badge visibility, but that's not required for v1.)

### Script structure (`run_check.py`)

- `Result` dataclass: `category`, `name`, `status` (`PASS`/`WARNING`/`FAIL`),
  `detail` (short human string), `duration_ms`.
- One function per category (`check_public_site()`, `check_inquiry_email()`,
  `check_sms_send()`, `check_sms_balance()`, `check_email_send()`,
  `check_email_balance()`, `check_other_features()`), each returning a
  list of `Result`. Every external call is wrapped in try/except so one
  network error becomes a single FAIL `Result`, not a crashed script.
- `render_html(results)`: builds the report — summary banner (worst
  status, counts per status, run timestamp in Asia/Colombo) followed by
  one section per category, each check rendered as a row/card with a
  colored badge.
- `send_report(html, subject)`: POSTs to `https://api.brevo.com/v3/smtp/email`
  using `BREVO_API_KEY`, from `info@microplex.lk` / "Microplex
  Corporation", to `REPORT_RECIPIENT`.
- `main()`: runs all checks, computes worst overall status for the
  subject line, renders, sends. Prints a plain-text summary to stdout
  for the Actions log regardless of email success.

### Check details

**1. Public site** — For each of `/`, `/Home/About`, `/Home/Solutions`,
`/Home/Contact`, `/Home/Privacy`: GET, PASS if 200 and a known marker
substring is present in the body; WARNING if 200 but response time
>3000ms or the marker is missing; FAIL on non-200, timeout, or connection
error.

**2. Solution integration email** — GET `/Home/Solutions` with a
`requests.Session()` to capture the antiforgery cookie and scrape the
`__RequestVerificationToken` hidden input value; POST `/Inquiry/Submit`
with `Service`, `Email=QA_TEST_EMAIL`, `ReturnUrl=/Home/Solutions`, and
the token. PASS if the response (after redirect) does not show the
`InquiryError` alert text; FAIL otherwise or on any request error.

**3. SMS Send API** — `POST /api/sms/send` with header
`api_token: QA_SMS_API_KEY`, body `{recipient: QA_TEST_PHONE, sender_id:
<a fixed approved sender id>, type: "plain", message: "Microplex health
check <UTC timestamp>"}`. PASS on `success:true`; WARNING if the body
says insufficient balance; FAIL on 401/502/timeout/other.

**4. SMS Balance API** — `GET /api/sms/balance` with
`api_token: QA_SMS_API_KEY`. PASS if 200 and `balance` present and above
threshold (`SMS_LOW_BALANCE_THRESHOLD`, default 10); WARNING if below
threshold; FAIL on error.

**5. Email Send API** — `POST /api/email/send` with header
`api_token: QA_EMAIL_API_KEY`, body `{recipient: QA_TEST_EMAIL, subject:
"Microplex health check", message: "<small HTML>"}`. Same
PASS/WARNING/FAIL rules as SMS Send.

**6. Email Balance API** — `GET /api/email/balance`, same shape as SMS
Balance with its own threshold (`EMAIL_LOW_BALANCE_THRESHOLD`, default
10).

**7. Other features** — GET `/Account/Login` expect 200. GET `/Clients`,
`/Dashboard`, `/ApiDocumentation/Sms` unauthenticated, expect a redirect
to `/Account/Login` (following redirects disabled so the 302 + Location
header can be checked directly) — FAIL if any of these return 200
(auth-gate regression) or a 5xx. GET a nonsense path (e.g.
`/this-does-not-exist-12345`) and expect a non-500 response (404 or the
app's own error page) — FAIL on 500.

## Report format

Single HTML file/string, inline CSS only (must render in Gmail clipped
at ~102KB, so kept lightweight — no external assets, no `<script>`).

- Header: "Microplex Daily Health Check — <date> <time> (Asia/Colombo)"
  plus a big status pill showing the worst status across all checks.
- Counts row: "X passed · Y warnings · Z failed".
- One section per category (matching the 7 items above) with a heading
  and a list of checks; each check is a row with: name, colored badge
  (`PASS` green `#16a34a`, `WARNING` orange `#d97706`, `FAIL` red
  `#dc2626`), detail text, and duration in ms.
- Footer: generated-by line noting this is an automated GitHub Actions
  run, with a link to the workflow run (`GITHUB_SERVER_URL`/`GITHUB_REPOSITORY`/
  `actions/runs`/`GITHUB_RUN_ID`, available as env vars in Actions) for
  drill-down.

Email subject reflects the worst status:
`✅ Microplex Health Check — All Passed`,
`⚠️ Microplex Health Check — N Warning(s)`, or
`🔴 Microplex Health Check — N Failed`.

## Secrets / configuration

Repo secrets (added by the user in GitHub Settings → Secrets, never
handled by Claude):

| Secret | Purpose |
|---|---|
| `QA_SMS_API_KEY` | SMS Send/Balance API auth for the QA test client |
| `QA_EMAIL_API_KEY` | Email Send/Balance API auth for the QA test client |
| `QA_TEST_PHONE` | Safe destination for the daily test SMS |
| `QA_TEST_EMAIL` | Safe destination for the daily test email + inquiry test |
| `QA_SMS_SENDER_ID` | Approved sender ID to use for the SMS send test |
| `BREVO_API_KEY` | Sends the report itself (reuses the existing production key) |
| `REPORT_RECIPIENT` | `contacts.nmssalman@gmail.com` (not sensitive, kept as a secret/var for easy rotation) |

`BASE_URL` (`https://microplex.lk`) and the low-balance thresholds are
plain constants in the script, not secrets.

## Error handling

- Every check function catches its own exceptions (timeout, connection
  error, unexpected response shape) and converts them to a FAIL `Result`
  with the exception message as detail — the script never crashes mid-run
  because one endpoint is down.
- If sending the report via Brevo itself fails, the script prints the
  full report to the Actions log (so the run is never silently lost) and
  exits non-zero, which surfaces as a failed workflow run — the one case
  where we do want GitHub's own failure notification, since otherwise
  the failure would be invisible.

## Testing plan

- Dry-run the script locally (not via cron) against production with a
  throwaway `--dry-run` flag that renders the HTML to a local file
  instead of emailing it, so the report design can be checked visually
  before wiring up real secrets/scheduling.
- After secrets are added, trigger the workflow manually via
  `workflow_dispatch` and confirm the report arrives correctly, with at
  least one deliberately-forced WARNING/FAIL (e.g. temporarily wrong API
  key) verified once to confirm the color-coding and detail text look
  right, then confirmed passing normally.
- Confirm the cron entry fires the next morning as expected.

## Open items before implementation can finish end-to-end

- User must create the QA test `Client` in the admin UI and supply the
  resulting values for the secrets table above (via GitHub Secrets, not
  chat).
- User must add all secrets listed above to the repository.
