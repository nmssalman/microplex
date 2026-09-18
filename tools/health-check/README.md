# Daily Health Check

Automated daily check of https://microplex.lk's public pages and SMS/Email
gateway APIs. Runs via `.github/workflows/daily-health-check.yml` at ~4:05 AM
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
