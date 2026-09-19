# Daily Health Check

Automated daily check of https://microplex.lk's public pages and SMS/Email
gateway APIs. Runs via `.github/workflows/daily-health-check.yml` at ~6:15 AM
Asia/Colombo every day, and emails a color-coded HTML report to the address
in the `REPORT_RECIPIENT` secret.

Full design: `docs/superpowers/specs/2026-09-19-daily-health-check-design.md`.

## One-time setup

1. In the Microplex admin panel, create a dedicated QA test `Client`
   (`/Clients/Create`) with both "Email Solution" and "SMS Solution"
   enabled, and give it some starting SMS/Email credits.
2. Generate its API keys under `/ApiDocumentation/Sms` and
   `/ApiDocumentation/Email`.
3. In this repository's Settings -> Secrets and variables -> Actions, add:

   | Secret | Value |
   |---|---|
   | `QA_SMS_API_KEY` | the QA client's SMS API key |
   | `QA_EMAIL_API_KEY` | the QA client's Email API key |
   | `QA_TEST_PHONE` | a safe phone number to receive the daily test SMS (E.164, e.g. `+94711111344`) |
   | `QA_TEST_EMAIL` | a safe inbox to receive the daily test email and inquiry confirmation |
   | `QA_SMS_SENDER_ID` | an approved SMS sender ID to send the test message from |
   | `BREVO_API_KEY` | the same Brevo API key already used in production (`Brevo:ApiKey`) |
   | `REPORT_RECIPIENT` | `contacts.nmssalman@gmail.com` |

### Keeping the schedule alive

GitHub Actions automatically **disables `schedule:` triggers after 60 days
with no commits to the repository** (GitHub emails the repo owner once when
this happens). If this repo goes quiet for two months, the daily check will
silently stop firing until someone pushes a commit or manually re-enables the
workflow from the Actions tab (select the workflow -> "..." -> "Enable
workflow"). There's no automated alert for this beyond GitHub's one-time
email, so it's worth checking the Actions tab occasionally on slow-moving
repos.

The `schedule:` trigger and `workflow_dispatch` (both the "Run workflow"
button in the Actions tab and `gh workflow run`) only work once this workflow
file exists on the **repository's default branch**. Merging this branch is
required before either can be used — a workflow file that only exists on a
feature branch will not run on a schedule and cannot be dispatched manually.

### Credit cost per run

Each run consumes real send credits, not just from this tool:

- The QA test client (configured above) spends **~1 SMS + 1 Email** on the
  `SMS Send API` and `Email Send API` checks.
- A **separate, different client is also charged**: the Solutions-page
  inquiry flow (`Controllers/InquiryController.cs`) sends two emails per run
  (a thank-you email and an internal notification) via
  `InternalEmailApiClient`, which authenticates with its own
  `InternalEmailApi:ApiToken` configured in the production app — a different
  key from the QA client's. This health check does not monitor that client's
  balance.

  If that client runs out of credit, the "Solution Integration Email" check
  (`Solution Integration Email :: Submit inquiry`) will fail with a message
  like "Inquiry form returned an error banner" plus a snippet of the actual
  error page — which can look like a code regression rather than a credit
  issue. If this specific check starts failing, check that client's SMS/Email
  balance in the admin panel before assuming the inquiry flow itself is
  broken.

## Running locally

```bash
cd tools/health-check
pip install -r requirements-dev.txt
export QA_SMS_API_KEY=... QA_EMAIL_API_KEY=... QA_TEST_PHONE=... \
       QA_TEST_EMAIL=... QA_SMS_SENDER_ID=... BREVO_API_KEY=... REPORT_RECIPIENT=...
python run_check.py --dry-run --output report.html   # writes the report locally instead of emailing it
python run_check.py                                   # runs for real and emails the report
```

## Running the tests

```bash
cd tools/health-check
pip install -r requirements-dev.txt
python -m pytest tests/ -v
```

## Triggering a run manually

Use the "Run workflow" button on the "Daily Health Check" workflow in the
Actions tab, or:

```bash
gh workflow run daily-health-check.yml
```
