# Daily Automated Health-Check Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a GitHub Actions workflow that runs every morning at ~4am Asia/Colombo, exercises microplex.lk's public pages and SMS/Email gateway APIs end-to-end, and emails a color-coded HTML pass/warning/fail report to `contacts.nmssalman@gmail.com`.

**Architecture:** A single Python script (`tools/health-check/run_check.py`) with one pure function per check category, each returning a list of `Result` records. `main()` runs them all, renders one self-contained HTML report, and sends it via the Brevo transactional email API (independent of the Microplex Email API being tested). A GitHub Actions workflow runs the script daily via `schedule:` cron plus `workflow_dispatch:` for manual runs, injecting secrets as environment variables.

**Tech Stack:** Python 3.12, `requests` (runtime), `pytest` (tests only), GitHub Actions (`ubuntu-latest`), Brevo transactional email API.

**Spec:** [docs/superpowers/specs/2026-09-19-daily-health-check-design.md](../specs/2026-09-19-daily-health-check-design.md)

## Global Constraints

- Target system under test: `https://microplex.lk` (constant `BASE_URL` default, overridable via `BASE_URL` env var).
- Schedule: cron `35 22 * * *` (UTC) ≈ 04:05 Asia/Colombo — not an exact top-of-hour mark, per the spec's anti-congestion guidance.
- Status values are exactly the strings `PASS`, `WARNING`, `FAIL`.
- Status colors: PASS `#16a34a`, WARNING `#d97706`, FAIL `#dc2626`.
- Low-balance warning thresholds: SMS and Email both default to `10` credits.
- Slow-response warning threshold for public pages: `3000` ms.
- Per-request timeout: `10.0` seconds.
- Report delivery: Brevo API `https://api.brevo.com/v3/smtp/email`, sender `info@microplex.lk` / `"Microplex Corporation"`.
- Required secrets/env vars: `QA_SMS_API_KEY`, `QA_EMAIL_API_KEY`, `QA_TEST_PHONE`, `QA_TEST_EMAIL`, `QA_SMS_SENDER_ID`, `BREVO_API_KEY`, `REPORT_RECIPIENT`.
- Check order (and report category order) matches the user's original numbered list exactly: Public Site → Solution Integration Email → SMS Send API → SMS Balance API → Email Send API → Email Balance API → Other Features.
- The script must never crash mid-run: every external call is wrapped so one bad endpoint becomes a single `FAIL` `Result`, not an exception.
- The workflow's own exit code is non-zero only when the report itself fails to send; individual `FAIL` results are communicated via the emailed report, not workflow failure.

---

## Task 1: Scaffolding — `Result`, `Config`, and shared constants

**Files:**
- Create: `tools/health-check/run_check.py`
- Create: `tools/health-check/tests/conftest.py`
- Create: `tools/health-check/tests/test_run_check.py`

**Interfaces:**
- Produces: `Result` dataclass (`category: str, name: str, status: str, detail: str, duration_ms: int`), `Config` dataclass with `from_env(env: dict | None = None) -> Config` static method, constants `PASS`, `WARNING`, `FAIL`, `STATUS_RANK: dict[str,int]`, `STATUS_COLOR: dict[str,str]`, `COLOMBO_TZ` (a `zoneinfo.ZoneInfo("Asia/Colombo")`).

- [ ] **Step 1: Write the failing tests**

Create `tools/health-check/tests/conftest.py`:

```python
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
```

Create `tools/health-check/tests/test_run_check.py`:

```python
import pytest

import run_check as rc


def make_config(**overrides) -> "rc.Config":
    base = dict(
        base_url="https://example.test",
        qa_sms_api_key="sms-key",
        qa_email_api_key="email-key",
        qa_test_phone="+94700000000",
        qa_test_email="qa@example.test",
        qa_sms_sender_id="MICROPLEX",
        brevo_api_key="brevo-key",
        report_recipient="report@example.test",
    )
    base.update(overrides)
    return rc.Config(**base)


REQUIRED_ENV = {
    "QA_SMS_API_KEY": "s",
    "QA_EMAIL_API_KEY": "e",
    "QA_TEST_PHONE": "+94700000000",
    "QA_TEST_EMAIL": "qa@example.test",
    "QA_SMS_SENDER_ID": "MICROPLEX",
    "BREVO_API_KEY": "b",
    "REPORT_RECIPIENT": "report@example.test",
}


def test_config_from_env_requires_qa_sms_api_key():
    env = {k: v for k, v in REQUIRED_ENV.items() if k != "QA_SMS_API_KEY"}
    with pytest.raises(KeyError):
        rc.Config.from_env(env)


def test_config_from_env_defaults_base_url():
    config = rc.Config.from_env(dict(REQUIRED_ENV))
    assert config.base_url == "https://microplex.lk"
    assert config.sms_low_balance_threshold == 10
    assert config.email_low_balance_threshold == 10


def test_config_from_env_builds_run_url_from_github_vars():
    env = dict(REQUIRED_ENV)
    env.update(
        GITHUB_SERVER_URL="https://github.com",
        GITHUB_REPOSITORY="acme/microplex",
        GITHUB_RUN_ID="123",
    )
    config = rc.Config.from_env(env)
    assert config.run_url == "https://github.com/acme/microplex/actions/runs/123"


def test_config_from_env_run_url_empty_when_not_in_actions():
    config = rc.Config.from_env(dict(REQUIRED_ENV))
    assert config.run_url == ""
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: FAIL (or ERROR) — `run_check.py` does not exist yet, so `import run_check` fails with `ModuleNotFoundError`.

- [ ] **Step 3: Write the minimal implementation**

Create `tools/health-check/run_check.py`:

```python
#!/usr/bin/env python3
"""Daily production health check for microplex.lk.

See docs/superpowers/specs/2026-09-19-daily-health-check-design.md.
"""
from __future__ import annotations

import os
from dataclasses import dataclass
from typing import Optional
from zoneinfo import ZoneInfo

COLOMBO_TZ = ZoneInfo("Asia/Colombo")

PASS = "PASS"
WARNING = "WARNING"
FAIL = "FAIL"

STATUS_RANK = {PASS: 0, WARNING: 1, FAIL: 2}
STATUS_COLOR = {PASS: "#16a34a", WARNING: "#d97706", FAIL: "#dc2626"}


@dataclass
class Result:
    category: str
    name: str
    status: str
    detail: str
    duration_ms: int


@dataclass
class Config:
    base_url: str
    qa_sms_api_key: str
    qa_email_api_key: str
    qa_test_phone: str
    qa_test_email: str
    qa_sms_sender_id: str
    brevo_api_key: str
    report_recipient: str
    sms_low_balance_threshold: int = 10
    email_low_balance_threshold: int = 10
    request_timeout_s: float = 10.0
    slow_response_ms: int = 3000
    run_url: str = ""

    @staticmethod
    def from_env(env: Optional[dict] = None) -> "Config":
        e = env if env is not None else os.environ
        server = e.get("GITHUB_SERVER_URL", "")
        repo = e.get("GITHUB_REPOSITORY", "")
        run_id = e.get("GITHUB_RUN_ID", "")
        run_url = f"{server}/{repo}/actions/runs/{run_id}" if server and repo and run_id else ""
        return Config(
            base_url=e.get("BASE_URL", "https://microplex.lk"),
            qa_sms_api_key=e["QA_SMS_API_KEY"],
            qa_email_api_key=e["QA_EMAIL_API_KEY"],
            qa_test_phone=e["QA_TEST_PHONE"],
            qa_test_email=e["QA_TEST_EMAIL"],
            qa_sms_sender_id=e["QA_SMS_SENDER_ID"],
            brevo_api_key=e["BREVO_API_KEY"],
            report_recipient=e["REPORT_RECIPIENT"],
            run_url=run_url,
        )
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (4 tests)

- [ ] **Step 5: Commit**

```bash
git add tools/health-check/run_check.py tools/health-check/tests/conftest.py tools/health-check/tests/test_run_check.py
git commit -m "Add Result/Config scaffolding for daily health check script"
```

---

## Task 2: `timed_request` helper

**Files:**
- Modify: `tools/health-check/run_check.py`
- Modify: `tools/health-check/tests/test_run_check.py`

**Interfaces:**
- Consumes: nothing new from Task 1 beyond the file itself.
- Produces: `timed_request(session: requests.Session, method: str, url: str, timeout: float, **kwargs) -> tuple[Optional[requests.Response], int, Optional[str]]` — `(response, duration_ms, error)`; `error` is `None` on success (regardless of HTTP status code) or the exception message string on a network-level failure, in which case `response` is `None`.

- [ ] **Step 1: Write the failing tests**

Add to `tools/health-check/tests/test_run_check.py`:

```python
from unittest.mock import MagicMock

import requests


def test_timed_request_success_returns_response_and_no_error():
    session = MagicMock(spec=requests.Session)
    fake_response = MagicMock(status_code=200)
    session.request.return_value = fake_response

    response, duration_ms, error = rc.timed_request(session, "GET", "https://x.test/", 5.0)

    assert response is fake_response
    assert error is None
    assert duration_ms >= 0
    session.request.assert_called_once_with("GET", "https://x.test/", timeout=5.0)


def test_timed_request_handles_request_exception():
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = requests.ConnectionError("boom")

    response, duration_ms, error = rc.timed_request(session, "GET", "https://x.test/", 5.0)

    assert response is None
    assert error == "boom"
    assert duration_ms >= 0


def test_timed_request_forwards_extra_kwargs():
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200)

    rc.timed_request(session, "POST", "https://x.test/", 5.0, json={"a": 1}, headers={"h": "v"})

    session.request.assert_called_once_with(
        "POST", "https://x.test/", timeout=5.0, json={"a": 1}, headers={"h": "v"}
    )
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/health-check && python -m pytest tests/ -v -k timed_request`
Expected: FAIL with `AttributeError: module 'run_check' has no attribute 'timed_request'`

- [ ] **Step 3: Write the minimal implementation**

Add to `tools/health-check/run_check.py` (after the `Config` class):

```python
import time

import requests


def timed_request(
    session: requests.Session, method: str, url: str, timeout: float, **kwargs
) -> tuple[Optional[requests.Response], int, Optional[str]]:
    start = time.monotonic()
    try:
        response = session.request(method, url, timeout=timeout, **kwargs)
        duration_ms = int((time.monotonic() - start) * 1000)
        return response, duration_ms, None
    except requests.RequestException as exc:
        duration_ms = int((time.monotonic() - start) * 1000)
        return None, duration_ms, str(exc)
```

Move the `import time` and `import requests` lines to the top of the file with the other imports.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (7 tests)

- [ ] **Step 5: Commit**

```bash
git add tools/health-check/run_check.py tools/health-check/tests/test_run_check.py
git commit -m "Add timed_request helper for health check script"
```

---

## Task 3: `check_public_site`

**Files:**
- Modify: `tools/health-check/run_check.py`
- Modify: `tools/health-check/tests/test_run_check.py`

**Interfaces:**
- Consumes: `Config`, `timed_request`, `Result`, `PASS`/`WARNING`/`FAIL` from Tasks 1-2.
- Produces: `check_public_site(config: Config, session: requests.Session) -> list[Result]` and constant `PUBLIC_PAGES: list[tuple[str, str]]` (path, expected marker substring pairs).

- [ ] **Step 1: Write the failing tests**

Add to `tools/health-check/tests/test_run_check.py`:

```python
def test_check_public_site_all_pass_when_pages_ok():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(
        status_code=200, text="Microplex SMS GATEWAY Contact Privacy"
    )

    results = rc.check_public_site(config, session)

    assert len(results) == len(rc.PUBLIC_PAGES)
    assert all(r.status == rc.PASS for r in results)
    assert all(r.category == "Public Site" for r in results)


def test_check_public_site_missing_marker_is_warning():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200, text="nothing relevant here")

    results = rc.check_public_site(config, session)

    assert all(r.status == rc.WARNING for r in results)


def test_check_public_site_non_200_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=500, text="error")

    results = rc.check_public_site(config, session)

    assert all(r.status == rc.FAIL for r in results)
    assert "500" in results[0].detail


def test_check_public_site_exception_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = requests.Timeout("timed out")

    results = rc.check_public_site(config, session)

    assert all(r.status == rc.FAIL for r in results)
    assert "timed out" in results[0].detail
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/health-check && python -m pytest tests/ -v -k check_public_site`
Expected: FAIL with `AttributeError: module 'run_check' has no attribute 'check_public_site'`

- [ ] **Step 3: Write the minimal implementation**

Add to `tools/health-check/run_check.py`:

```python
PUBLIC_PAGES = [
    ("/", "Microplex"),
    ("/Home/About", "Microplex"),
    ("/Home/Solutions", "SMS GATEWAY"),
    ("/Home/Contact", "Contact"),
    ("/Home/Privacy", "Privacy"),
]


def check_public_site(config: Config, session: requests.Session) -> list[Result]:
    results: list[Result] = []
    for path, marker in PUBLIC_PAGES:
        url = config.base_url + path
        name = f"GET {path}"
        response, duration_ms, error = timed_request(session, "GET", url, config.request_timeout_s)
        if error is not None:
            results.append(Result("Public Site", name, FAIL, error, duration_ms))
            continue
        if response.status_code != 200:
            results.append(Result("Public Site", name, FAIL, f"HTTP {response.status_code}", duration_ms))
            continue
        if marker not in response.text:
            results.append(
                Result("Public Site", name, WARNING, f"Expected marker '{marker}' not found", duration_ms)
            )
            continue
        if duration_ms > config.slow_response_ms:
            results.append(Result("Public Site", name, WARNING, f"Slow response ({duration_ms}ms)", duration_ms))
            continue
        results.append(Result("Public Site", name, PASS, f"HTTP 200 in {duration_ms}ms", duration_ms))
    return results
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (11 tests)

- [ ] **Step 5: Commit**

```bash
git add tools/health-check/run_check.py tools/health-check/tests/test_run_check.py
git commit -m "Add public site page check"
```

---

## Task 4: `check_inquiry_email` (Solution integration email test)

**Files:**
- Modify: `tools/health-check/run_check.py`
- Modify: `tools/health-check/tests/test_run_check.py`

**Interfaces:**
- Consumes: `Config`, `timed_request`, `Result`, status constants.
- Produces: `check_inquiry_email(config: Config, session: requests.Session) -> list[Result]`.

- [ ] **Step 1: Write the failing tests**

Add to `tools/health-check/tests/test_run_check.py`:

```python
TOKEN_HTML = '<input name="__RequestVerificationToken" type="hidden" value="tok123" />'


def test_check_inquiry_email_pass():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    get_response = MagicMock(status_code=200, text=TOKEN_HTML)
    post_response = MagicMock(status_code=200, text="<div>Thanks for reaching out!</div>")
    session.request.side_effect = [get_response, post_response]

    results = rc.check_inquiry_email(config, session)

    assert len(results) == 1
    assert results[0].status == rc.PASS
    assert results[0].category == "Solution Integration Email"


def test_check_inquiry_email_missing_token_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200, text="<html>no token here</html>")

    results = rc.check_inquiry_email(config, session)

    assert results[0].status == rc.FAIL
    assert "token" in results[0].detail.lower()


def test_check_inquiry_email_page_load_failure_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=500, text="error")

    results = rc.check_inquiry_email(config, session)

    assert results[0].status == rc.FAIL
    assert "500" in results[0].detail


def test_check_inquiry_email_error_banner_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    get_response = MagicMock(status_code=200, text=TOKEN_HTML)
    post_response = MagicMock(status_code=200, text='<div class="alert-danger">Something went wrong</div>')
    session.request.side_effect = [get_response, post_response]

    results = rc.check_inquiry_email(config, session)

    assert results[0].status == rc.FAIL


def test_check_inquiry_email_submit_exception_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    get_response = MagicMock(status_code=200, text=TOKEN_HTML)
    session.request.side_effect = [get_response, requests.ConnectionError("no route")]

    results = rc.check_inquiry_email(config, session)

    assert results[0].status == rc.FAIL
    assert "no route" in results[0].detail
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/health-check && python -m pytest tests/ -v -k check_inquiry_email`
Expected: FAIL with `AttributeError: module 'run_check' has no attribute 'check_inquiry_email'`

- [ ] **Step 3: Write the minimal implementation**

Add to `tools/health-check/run_check.py`:

```python
import re

TOKEN_RE = re.compile(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"')


def check_inquiry_email(config: Config, session: requests.Session) -> list[Result]:
    category = "Solution Integration Email"
    page_url = config.base_url + "/Home/Solutions"
    page_response, page_ms, page_error = timed_request(session, "GET", page_url, config.request_timeout_s)
    if page_error is not None:
        return [Result(category, "Submit inquiry", FAIL, page_error, page_ms)]
    if page_response.status_code != 200:
        return [Result(category, "Submit inquiry", FAIL, f"HTTP {page_response.status_code} loading Solutions page", page_ms)]

    match = TOKEN_RE.search(page_response.text)
    if not match:
        return [Result(category, "Submit inquiry", FAIL, "Antiforgery token not found on Solutions page", page_ms)]
    token = match.group(1)

    submit_url = config.base_url + "/Inquiry/Submit"
    payload = {
        "Service": "SMS Gateway",
        "Email": config.qa_test_email,
        "ReturnUrl": "/Home/Solutions",
        "__RequestVerificationToken": token,
    }
    submit_response, submit_ms, submit_error = timed_request(
        session, "POST", submit_url, config.request_timeout_s, data=payload, allow_redirects=True
    )
    total_ms = page_ms + submit_ms
    if submit_error is not None:
        return [Result(category, "Submit inquiry", FAIL, submit_error, total_ms)]
    if submit_response.status_code >= 400:
        return [Result(category, "Submit inquiry", FAIL, f"HTTP {submit_response.status_code}", total_ms)]
    if "alert-danger" in submit_response.text:
        return [Result(category, "Submit inquiry", FAIL, "Inquiry form returned an error banner", total_ms)]
    return [Result(category, "Submit inquiry", PASS, f"Inquiry accepted in {total_ms}ms", total_ms)]
```

Move `import re` to the top of the file with the other imports.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (16 tests)

- [ ] **Step 5: Commit**

```bash
git add tools/health-check/run_check.py tools/health-check/tests/test_run_check.py
git commit -m "Add solution integration email check"
```

---

## Task 5: `check_sms_send` and `check_sms_balance`

**Files:**
- Modify: `tools/health-check/run_check.py`
- Modify: `tools/health-check/tests/test_run_check.py`

**Interfaces:**
- Consumes: `Config`, `timed_request`, `Result`, status constants.
- Produces: `check_sms_send(config: Config, session: requests.Session) -> list[Result]`, `check_sms_balance(config: Config, session: requests.Session) -> list[Result]`.

- [ ] **Step 1: Write the failing tests**

Add to `tools/health-check/tests/test_run_check.py`:

```python
def test_check_sms_send_pass():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200, text='{"success": true}')

    results = rc.check_sms_send(config, session)

    assert len(results) == 1
    assert results[0].status == rc.PASS
    assert results[0].category == "SMS Send API"


def test_check_sms_send_insufficient_balance_is_warning():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(
        status_code=400, text='{"success": false, "message": "Insufficient SMS balance."}'
    )

    results = rc.check_sms_send(config, session)

    assert results[0].status == rc.WARNING


def test_check_sms_send_gateway_error_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=502, text='{"success": false}')

    results = rc.check_sms_send(config, session)

    assert results[0].status == rc.FAIL


def test_check_sms_send_exception_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = requests.Timeout("timed out")

    results = rc.check_sms_send(config, session)

    assert results[0].status == rc.FAIL
    assert "timed out" in results[0].detail


def test_check_sms_balance_pass():
    config = make_config(sms_low_balance_threshold=10)
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200, json=lambda: {"success": True, "balance": 50})

    results = rc.check_sms_balance(config, session)

    assert results[0].status == rc.PASS
    assert results[0].category == "SMS Balance API"


def test_check_sms_balance_low_is_warning():
    config = make_config(sms_low_balance_threshold=10)
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200, json=lambda: {"success": True, "balance": 3})

    results = rc.check_sms_balance(config, session)

    assert results[0].status == rc.WARNING


def test_check_sms_balance_unauthorized_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=401, text='{"success": false}')

    results = rc.check_sms_balance(config, session)

    assert results[0].status == rc.FAIL
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/health-check && python -m pytest tests/ -v -k "check_sms"`
Expected: FAIL with `AttributeError: module 'run_check' has no attribute 'check_sms_send'`

- [ ] **Step 3: Write the minimal implementation**

Add to `tools/health-check/run_check.py`:

```python
from datetime import datetime


def check_sms_send(config: Config, session: requests.Session) -> list[Result]:
    category = "SMS Send API"
    name = "POST /api/sms/send"
    url = config.base_url + "/api/sms/send"
    headers = {"api_token": config.qa_sms_api_key}
    payload = {
        "recipient": config.qa_test_phone,
        "sender_id": config.qa_sms_sender_id,
        "type": "plain",
        "message": f"Microplex health check {datetime.utcnow().isoformat(timespec='seconds')}Z",
    }
    response, duration_ms, error = timed_request(
        session, "POST", url, config.request_timeout_s, json=payload, headers=headers
    )
    if error is not None:
        return [Result(category, name, FAIL, error, duration_ms)]
    if response.status_code == 200:
        return [Result(category, name, PASS, f"Sent in {duration_ms}ms", duration_ms)]
    if response.status_code == 400 and "balance" in response.text.lower():
        return [Result(category, name, WARNING, "Insufficient SMS balance — top up the QA client", duration_ms)]
    return [Result(category, name, FAIL, f"HTTP {response.status_code}: {response.text[:200]}", duration_ms)]


def check_sms_balance(config: Config, session: requests.Session) -> list[Result]:
    category = "SMS Balance API"
    name = "GET /api/sms/balance"
    url = config.base_url + "/api/sms/balance"
    headers = {"api_token": config.qa_sms_api_key}
    response, duration_ms, error = timed_request(session, "GET", url, config.request_timeout_s, headers=headers)
    if error is not None:
        return [Result(category, name, FAIL, error, duration_ms)]
    if response.status_code != 200:
        return [Result(category, name, FAIL, f"HTTP {response.status_code}: {response.text[:200]}", duration_ms)]
    body = response.json()
    balance = body.get("balance")
    if balance is None:
        return [Result(category, name, FAIL, "Response missing 'balance' field", duration_ms)]
    if balance < config.sms_low_balance_threshold:
        return [Result(category, name, WARNING, f"Balance low: {balance} credits", duration_ms)]
    return [Result(category, name, PASS, f"Balance: {balance} credits", duration_ms)]
```

Move `from datetime import datetime` to the top of the file with the other imports.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (23 tests)

- [ ] **Step 5: Commit**

```bash
git add tools/health-check/run_check.py tools/health-check/tests/test_run_check.py
git commit -m "Add SMS send and balance API checks"
```

---

## Task 6: `check_email_send` and `check_email_balance`

**Files:**
- Modify: `tools/health-check/run_check.py`
- Modify: `tools/health-check/tests/test_run_check.py`

**Interfaces:**
- Consumes: `Config`, `timed_request`, `Result`, status constants.
- Produces: `check_email_send(config: Config, session: requests.Session) -> list[Result]`, `check_email_balance(config: Config, session: requests.Session) -> list[Result]`.

- [ ] **Step 1: Write the failing tests**

Add to `tools/health-check/tests/test_run_check.py`:

```python
def test_check_email_send_pass():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200, text='{"success": true}')

    results = rc.check_email_send(config, session)

    assert len(results) == 1
    assert results[0].status == rc.PASS
    assert results[0].category == "Email Send API"


def test_check_email_send_insufficient_balance_is_warning():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(
        status_code=400, text='{"success": false, "message": "Insufficient Email balance."}'
    )

    results = rc.check_email_send(config, session)

    assert results[0].status == rc.WARNING


def test_check_email_send_gateway_error_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=502, text='{"success": false}')

    results = rc.check_email_send(config, session)

    assert results[0].status == rc.FAIL


def test_check_email_balance_pass():
    config = make_config(email_low_balance_threshold=10)
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200, json=lambda: {"success": True, "balance": 50})

    results = rc.check_email_balance(config, session)

    assert results[0].status == rc.PASS
    assert results[0].category == "Email Balance API"


def test_check_email_balance_low_is_warning():
    config = make_config(email_low_balance_threshold=10)
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200, json=lambda: {"success": True, "balance": 2})

    results = rc.check_email_balance(config, session)

    assert results[0].status == rc.WARNING


def test_check_email_balance_unauthorized_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=401, text='{"success": false}')

    results = rc.check_email_balance(config, session)

    assert results[0].status == rc.FAIL
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/health-check && python -m pytest tests/ -v -k "check_email"`
Expected: FAIL with `AttributeError: module 'run_check' has no attribute 'check_email_send'`

- [ ] **Step 3: Write the minimal implementation**

Add to `tools/health-check/run_check.py`:

```python
def check_email_send(config: Config, session: requests.Session) -> list[Result]:
    category = "Email Send API"
    name = "POST /api/email/send"
    url = config.base_url + "/api/email/send"
    headers = {"api_token": config.qa_email_api_key}
    payload = {
        "recipient": config.qa_test_email,
        "subject": "Microplex health check",
        "message": f"<p>Automated health check at {datetime.utcnow().isoformat(timespec='seconds')}Z</p>",
    }
    response, duration_ms, error = timed_request(
        session, "POST", url, config.request_timeout_s, json=payload, headers=headers
    )
    if error is not None:
        return [Result(category, name, FAIL, error, duration_ms)]
    if response.status_code == 200:
        return [Result(category, name, PASS, f"Sent in {duration_ms}ms", duration_ms)]
    if response.status_code == 400 and "balance" in response.text.lower():
        return [Result(category, name, WARNING, "Insufficient Email balance — top up the QA client", duration_ms)]
    return [Result(category, name, FAIL, f"HTTP {response.status_code}: {response.text[:200]}", duration_ms)]


def check_email_balance(config: Config, session: requests.Session) -> list[Result]:
    category = "Email Balance API"
    name = "GET /api/email/balance"
    url = config.base_url + "/api/email/balance"
    headers = {"api_token": config.qa_email_api_key}
    response, duration_ms, error = timed_request(session, "GET", url, config.request_timeout_s, headers=headers)
    if error is not None:
        return [Result(category, name, FAIL, error, duration_ms)]
    if response.status_code != 200:
        return [Result(category, name, FAIL, f"HTTP {response.status_code}: {response.text[:200]}", duration_ms)]
    body = response.json()
    balance = body.get("balance")
    if balance is None:
        return [Result(category, name, FAIL, "Response missing 'balance' field", duration_ms)]
    if balance < config.email_low_balance_threshold:
        return [Result(category, name, WARNING, f"Balance low: {balance} credits", duration_ms)]
    return [Result(category, name, PASS, f"Balance: {balance} credits", duration_ms)]
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (29 tests)

- [ ] **Step 5: Commit**

```bash
git add tools/health-check/run_check.py tools/health-check/tests/test_run_check.py
git commit -m "Add Email send and balance API checks"
```

---

## Task 7: `check_other_features`

**Files:**
- Modify: `tools/health-check/run_check.py`
- Modify: `tools/health-check/tests/test_run_check.py`

**Interfaces:**
- Consumes: `Config`, `timed_request`, `Result`, status constants.
- Produces: `check_other_features(config: Config, session: requests.Session) -> list[Result]`, constant `PROTECTED_PATHS: list[str]`.

- [ ] **Step 1: Write the failing tests**

Add to `tools/health-check/tests/test_run_check.py`:

```python
def _other_features_fake_request(login_status=200, protected_status=302, protected_location="/Account/Login", unknown_status=404):
    def fake_request(method, url, timeout=None, allow_redirects=None, **kwargs):
        if url.endswith("/Account/Login"):
            return MagicMock(status_code=login_status)
        if url.endswith("/this-does-not-exist-12345"):
            return MagicMock(status_code=unknown_status)
        return MagicMock(status_code=protected_status, headers={"Location": protected_location})

    return fake_request


def test_check_other_features_all_good():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = _other_features_fake_request()

    results = rc.check_other_features(config, session)

    assert len(results) == 2 + len(rc.PROTECTED_PATHS)
    assert all(r.status == rc.PASS for r in results)


def test_check_other_features_detects_auth_gate_regression():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = _other_features_fake_request(protected_status=200, protected_location="")

    results = rc.check_other_features(config, session)

    gate_results = [r for r in results if r.name.startswith("Auth gate")]
    assert len(gate_results) == len(rc.PROTECTED_PATHS)
    assert all(r.status == rc.FAIL for r in gate_results)


def test_check_other_features_500_on_unknown_path_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = _other_features_fake_request(unknown_status=500)

    results = rc.check_other_features(config, session)

    unknown_result = next(r for r in results if r.name == "Unknown path handling")
    assert unknown_result.status == rc.FAIL


def test_check_other_features_login_page_down_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = _other_features_fake_request(login_status=500)

    results = rc.check_other_features(config, session)

    login_result = next(r for r in results if r.name == "Login page reachable")
    assert login_result.status == rc.FAIL
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/health-check && python -m pytest tests/ -v -k check_other_features`
Expected: FAIL with `AttributeError: module 'run_check' has no attribute 'check_other_features'`

- [ ] **Step 3: Write the minimal implementation**

Add to `tools/health-check/run_check.py`:

```python
PROTECTED_PATHS = ["/Clients", "/Dashboard", "/ApiDocumentation/Sms"]


def check_other_features(config: Config, session: requests.Session) -> list[Result]:
    category = "Other Features"
    results: list[Result] = []

    login_url = config.base_url + "/Account/Login"
    response, duration_ms, error = timed_request(session, "GET", login_url, config.request_timeout_s)
    if error is not None:
        results.append(Result(category, "Login page reachable", FAIL, error, duration_ms))
    elif response.status_code != 200:
        results.append(Result(category, "Login page reachable", FAIL, f"HTTP {response.status_code}", duration_ms))
    else:
        results.append(Result(category, "Login page reachable", PASS, f"HTTP 200 in {duration_ms}ms", duration_ms))

    for path in PROTECTED_PATHS:
        page_url = config.base_url + path
        name = f"Auth gate on {path}"
        response, duration_ms, error = timed_request(
            session, "GET", page_url, config.request_timeout_s, allow_redirects=False
        )
        if error is not None:
            results.append(Result(category, name, FAIL, error, duration_ms))
            continue
        location = response.headers.get("Location", "")
        if response.status_code in (301, 302, 303, 307, 308) and "Account/Login" in location:
            results.append(Result(category, name, PASS, f"Redirected to login ({response.status_code})", duration_ms))
        elif response.status_code == 200:
            results.append(Result(category, name, FAIL, "Protected page returned 200 without authentication", duration_ms))
        else:
            results.append(Result(category, name, FAIL, f"Unexpected HTTP {response.status_code}", duration_ms))

    unknown_url = config.base_url + "/this-does-not-exist-12345"
    name = "Unknown path handling"
    response, duration_ms, error = timed_request(session, "GET", unknown_url, config.request_timeout_s)
    if error is not None:
        results.append(Result(category, name, FAIL, error, duration_ms))
    elif response.status_code >= 500:
        results.append(Result(category, name, FAIL, f"HTTP {response.status_code} on unknown path", duration_ms))
    else:
        results.append(Result(category, name, PASS, f"HTTP {response.status_code} on unknown path", duration_ms))

    return results
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (33 tests)

- [ ] **Step 5: Commit**

```bash
git add tools/health-check/run_check.py tools/health-check/tests/test_run_check.py
git commit -m "Add other-features check (login, auth gate, unknown path)"
```

---

## Task 8: `render_html`, `compute_overall_status`, `build_subject`

**Files:**
- Modify: `tools/health-check/run_check.py`
- Modify: `tools/health-check/tests/test_run_check.py`

**Interfaces:**
- Consumes: `Result`, `STATUS_RANK`, `STATUS_COLOR`, `PASS`/`WARNING`/`FAIL`.
- Produces: `compute_overall_status(results: list[Result]) -> str`, `build_subject(overall_status: str, results: list[Result]) -> str`, `render_html(results: list[Result], generated_at: datetime, run_url: str) -> str`.

- [ ] **Step 1: Write the failing tests**

Add to `tools/health-check/tests/test_run_check.py`:

```python
def test_compute_overall_status_worst_wins():
    results = [rc.Result("c", "a", rc.PASS, "", 1), rc.Result("c", "b", rc.WARNING, "", 1)]
    assert rc.compute_overall_status(results) == rc.WARNING

    results.append(rc.Result("c", "d", rc.FAIL, "", 1))
    assert rc.compute_overall_status(results) == rc.FAIL


def test_compute_overall_status_empty_is_pass():
    assert rc.compute_overall_status([]) == rc.PASS


def test_build_subject_all_passed():
    results = [rc.Result("c", "a", rc.PASS, "", 1)]
    assert rc.build_subject(rc.PASS, results) == "✅ Microplex Health Check — All Passed"


def test_build_subject_warnings():
    results = [rc.Result("c", "a", rc.WARNING, "", 1), rc.Result("c", "b", rc.WARNING, "", 1)]
    assert rc.build_subject(rc.WARNING, results) == "⚠️ Microplex Health Check — 2 Warning(s)"


def test_build_subject_failures():
    results = [rc.Result("c", "a", rc.FAIL, "", 1)]
    assert rc.build_subject(rc.FAIL, results) == "\U0001f534 Microplex Health Check — 1 Failed"


def test_render_html_includes_names_categories_and_colors():
    from datetime import datetime as dt

    results = [
        rc.Result("Public Site", "GET /", rc.PASS, "HTTP 200 in 120ms", 120),
        rc.Result("SMS Send API", "POST /api/sms/send", rc.FAIL, "HTTP 502", 300),
    ]

    output = rc.render_html(results, dt(2026, 9, 19, 4, 5), "")

    assert "GET /" in output
    assert "Public Site" in output
    assert "SMS Send API" in output
    assert rc.STATUS_COLOR[rc.PASS] in output
    assert rc.STATUS_COLOR[rc.FAIL] in output
    assert "2026-09-19" in output


def test_render_html_includes_run_link_when_provided():
    from datetime import datetime as dt

    results = [rc.Result("Public Site", "GET /", rc.PASS, "ok", 10)]
    output = rc.render_html(results, dt(2026, 9, 19, 4, 5), "https://github.com/acme/microplex/actions/runs/123")

    assert "https://github.com/acme/microplex/actions/runs/123" in output


def test_render_html_omits_run_link_when_empty():
    from datetime import datetime as dt

    results = [rc.Result("Public Site", "GET /", rc.PASS, "ok", 10)]
    output = rc.render_html(results, dt(2026, 9, 19, 4, 5), "")

    assert "View this run" not in output
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/health-check && python -m pytest tests/ -v -k "overall_status or build_subject or render_html"`
Expected: FAIL with `AttributeError: module 'run_check' has no attribute 'compute_overall_status'`

- [ ] **Step 3: Write the minimal implementation**

Add to `tools/health-check/run_check.py`:

```python
import html as html_module


def compute_overall_status(results: list[Result]) -> str:
    if not results:
        return PASS
    return max((r.status for r in results), key=lambda s: STATUS_RANK[s])


def build_subject(overall_status: str, results: list[Result]) -> str:
    warnings = sum(1 for r in results if r.status == WARNING)
    failures = sum(1 for r in results if r.status == FAIL)
    if overall_status == PASS:
        return "✅ Microplex Health Check — All Passed"
    if overall_status == WARNING:
        return f"⚠️ Microplex Health Check — {warnings} Warning(s)"
    return f"\U0001f534 Microplex Health Check — {failures} Failed"


def _render_row(r: Result) -> str:
    return (
        '<tr style="border-bottom:1px solid #e5e7eb;">'
        f'<td style="padding:8px 4px;font:14px system-ui;color:#111827;">{html_module.escape(r.name)}</td>'
        '<td style="padding:8px 4px;white-space:nowrap;">'
        f'<span style="padding:3px 10px;border-radius:999px;color:#fff;font:600 12px system-ui;'
        f'background:{STATUS_COLOR[r.status]};">{r.status}</span></td>'
        f'<td style="padding:8px 4px;font:13px system-ui;color:#6b7280;">'
        f'{html_module.escape(r.detail)} ({r.duration_ms}ms)</td>'
        '</tr>'
    )


def render_html(results: list[Result], generated_at: datetime, run_url: str) -> str:
    overall = compute_overall_status(results)
    passed = sum(1 for r in results if r.status == PASS)
    warnings = sum(1 for r in results if r.status == WARNING)
    failures = sum(1 for r in results if r.status == FAIL)

    seen_categories: list[str] = []
    for r in results:
        if r.category not in seen_categories:
            seen_categories.append(r.category)

    sections = []
    for category in seen_categories:
        rows = "".join(_render_row(r) for r in results if r.category == category)
        sections.append(
            f'<h2 style="font:600 16px system-ui;margin:24px 0 8px;color:#111827;">'
            f'{html_module.escape(category)}</h2>'
            f'<table style="width:100%;border-collapse:collapse;">{rows}</table>'
        )

    run_link = ""
    if run_url:
        run_link = (
            f'<p style="font:12px system-ui;color:#6b7280;margin-top:24px;">'
            f'<a href="{html_module.escape(run_url)}" style="color:#6b7280;">View this run</a></p>'
        )

    return f"""<!DOCTYPE html>
<html><body style="margin:0;padding:24px;background:#f3f4f6;font-family:system-ui,-apple-system,sans-serif;">
<div style="max-width:640px;margin:0 auto;background:#ffffff;border-radius:8px;padding:24px;">
  <h1 style="font-size:20px;margin:0 0 8px;color:#111827;">Microplex Daily Health Check</h1>
  <p style="font:14px system-ui;color:#6b7280;margin:0 0 16px;">
    {generated_at.strftime('%Y-%m-%d %H:%M')} (Asia/Colombo)
  </p>
  <div style="display:inline-block;padding:6px 14px;border-radius:999px;color:#fff;font:600 13px system-ui;background:{STATUS_COLOR[overall]};">
    {overall}
  </div>
  <p style="font:14px system-ui;color:#374151;margin-top:12px;">
    {passed} passed &middot; {warnings} warning(s) &middot; {failures} failed
  </p>
  {''.join(sections)}
  {run_link}
</div>
</body></html>"""
```

Move `import html as html_module` to the top of the file with the other imports.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (41 tests)

- [ ] **Step 5: Commit**

```bash
git add tools/health-check/run_check.py tools/health-check/tests/test_run_check.py
git commit -m "Add HTML report rendering and subject line logic"
```

---

## Task 9: `send_report`

**Files:**
- Modify: `tools/health-check/run_check.py`
- Modify: `tools/health-check/tests/test_run_check.py`

**Interfaces:**
- Consumes: `Config`.
- Produces: `send_report(config: Config, html_body: str, subject: str) -> tuple[bool, str]` — `(success, error_detail)`; `error_detail` is `""` on success.

- [ ] **Step 1: Write the failing tests**

Add to `tools/health-check/tests/test_run_check.py`:

```python
from unittest.mock import patch


@patch("run_check.requests.post")
def test_send_report_success(mock_post):
    mock_post.return_value = MagicMock(status_code=201)
    config = make_config()

    sent, error = rc.send_report(config, "<html></html>", "Subject")

    assert sent is True
    assert error == ""
    _, kwargs = mock_post.call_args
    assert kwargs["headers"]["api-key"] == "brevo-key"
    assert kwargs["json"]["subject"] == "Subject"
    assert kwargs["json"]["to"] == [{"email": "report@example.test"}]


@patch("run_check.requests.post")
def test_send_report_failure_status(mock_post):
    mock_post.return_value = MagicMock(status_code=400, text="bad request")
    config = make_config()

    sent, error = rc.send_report(config, "<html></html>", "Subject")

    assert sent is False
    assert "400" in error


@patch("run_check.requests.post")
def test_send_report_network_error(mock_post):
    mock_post.side_effect = requests.ConnectionError("no network")
    config = make_config()

    sent, error = rc.send_report(config, "<html></html>", "Subject")

    assert sent is False
    assert "no network" in error
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/health-check && python -m pytest tests/ -v -k send_report`
Expected: FAIL with `AttributeError: module 'run_check' has no attribute 'send_report'`

- [ ] **Step 3: Write the minimal implementation**

Add to `tools/health-check/run_check.py`:

```python
def send_report(config: Config, html_body: str, subject: str) -> tuple[bool, str]:
    payload = {
        "sender": {"name": "Microplex Corporation", "email": "info@microplex.lk"},
        "to": [{"email": config.report_recipient}],
        "subject": subject,
        "htmlContent": html_body,
    }
    try:
        response = requests.post(
            "https://api.brevo.com/v3/smtp/email",
            json=payload,
            headers={"api-key": config.brevo_api_key, "accept": "application/json"},
            timeout=config.request_timeout_s,
        )
    except requests.RequestException as exc:
        return False, str(exc)
    if response.status_code >= 300:
        return False, f"Brevo returned HTTP {response.status_code}: {response.text[:300]}"
    return True, ""
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (44 tests)

- [ ] **Step 5: Commit**

```bash
git add tools/health-check/run_check.py tools/health-check/tests/test_run_check.py
git commit -m "Add Brevo report delivery"
```

---

## Task 10: `run_all_checks`, `print_summary`, and `main()` CLI

**Files:**
- Modify: `tools/health-check/run_check.py`
- Modify: `tools/health-check/tests/test_run_check.py`

**Interfaces:**
- Consumes: every `check_*` function, `render_html`, `compute_overall_status`, `build_subject`, `send_report`, `Config`.
- Produces: `CHECKS: list[Callable]`, `run_all_checks(config: Config) -> list[Result]`, `print_summary(results: list[Result]) -> None`, `main(argv: list[str] | None = None) -> int`.

- [ ] **Step 1: Write the failing tests**

Add to `tools/health-check/tests/test_run_check.py`:

```python
def _set_required_env(monkeypatch):
    for key, value in REQUIRED_ENV.items():
        monkeypatch.setenv(key, value)


def test_checks_run_in_user_requested_order():
    assert rc.CHECKS == [
        rc.check_public_site,
        rc.check_inquiry_email,
        rc.check_sms_send,
        rc.check_sms_balance,
        rc.check_email_send,
        rc.check_email_balance,
        rc.check_other_features,
    ]


def test_main_dry_run_writes_report(tmp_path, monkeypatch):
    _set_required_env(monkeypatch)
    monkeypatch.setattr(rc, "run_all_checks", lambda config: [rc.Result("c", "a", rc.PASS, "ok", 10)])
    output_path = tmp_path / "report.html"

    exit_code = rc.main(["--dry-run", "--output", str(output_path)])

    assert exit_code == 0
    assert output_path.exists()
    assert "Microplex Daily Health Check" in output_path.read_text()


def test_main_returns_nonzero_when_send_fails(monkeypatch):
    _set_required_env(monkeypatch)
    monkeypatch.setattr(rc, "run_all_checks", lambda config: [rc.Result("c", "a", rc.FAIL, "boom", 10)])
    monkeypatch.setattr(rc, "send_report", lambda config, html_body, subject: (False, "brevo down"))

    exit_code = rc.main([])

    assert exit_code == 1


def test_main_returns_zero_when_checks_fail_but_send_succeeds(monkeypatch):
    _set_required_env(monkeypatch)
    monkeypatch.setattr(rc, "run_all_checks", lambda config: [rc.Result("c", "a", rc.FAIL, "boom", 10)])
    monkeypatch.setattr(rc, "send_report", lambda config, html_body, subject: (True, ""))

    exit_code = rc.main([])

    assert exit_code == 0


def test_run_all_checks_aggregates_all_categories():
    config = make_config()
    session_results = [rc.Result("c", "a", rc.PASS, "ok", 1)]
    monkeypatched = [lambda c, s: session_results for _ in rc.CHECKS]
    original_checks = rc.CHECKS
    rc.CHECKS = monkeypatched
    try:
        results = rc.run_all_checks(config)
    finally:
        rc.CHECKS = original_checks
    assert len(results) == len(monkeypatched)
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/health-check && python -m pytest tests/ -v -k "CHECKS or main or run_all_checks"`
Expected: FAIL with `AttributeError: module 'run_check' has no attribute 'CHECKS'`

- [ ] **Step 3: Write the minimal implementation**

Add to `tools/health-check/run_check.py`:

```python
import argparse
import sys

CHECKS = [
    check_public_site,
    check_inquiry_email,
    check_sms_send,
    check_sms_balance,
    check_email_send,
    check_email_balance,
    check_other_features,
]


def run_all_checks(config: Config) -> list[Result]:
    session = requests.Session()
    results: list[Result] = []
    for check in CHECKS:
        results.extend(check(config, session))
    return results


def print_summary(results: list[Result]) -> None:
    for r in results:
        print(f"[{r.status:7}] {r.category} :: {r.name} — {r.detail} ({r.duration_ms}ms)")


def main(argv: Optional[list[str]] = None) -> int:
    parser = argparse.ArgumentParser(description="Microplex daily health check")
    parser.add_argument("--dry-run", action="store_true", help="Write the report to --output instead of emailing it")
    parser.add_argument("--output", default="health-check-report.html", help="Path to write the report in --dry-run mode")
    args = parser.parse_args(argv)

    config = Config.from_env()
    results = run_all_checks(config)
    print_summary(results)

    generated_at = datetime.now(COLOMBO_TZ)
    subject = build_subject(compute_overall_status(results), results)
    report_html = render_html(results, generated_at, config.run_url)

    if args.dry_run:
        with open(args.output, "w", encoding="utf-8") as f:
            f.write(report_html)
        print(f"Dry run: report written to {args.output}")
        return 0

    sent, error = send_report(config, report_html, subject)
    if not sent:
        print(f"FAILED TO SEND REPORT: {error}", file=sys.stderr)
        print("---- report body follows ----")
        print(report_html)
        return 1
    print("Report sent successfully.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
```

Move `import argparse` and `import sys` to the top of the file with the other imports.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (49 tests)

- [ ] **Step 5: Commit**

```bash
git add tools/health-check/run_check.py tools/health-check/tests/test_run_check.py
git commit -m "Wire up main() CLI and check ordering for health check script"
```

---

## Task 11: GitHub Actions workflow, dependencies, and setup docs

**Files:**
- Create: `.github/workflows/daily-health-check.yml`
- Create: `tools/health-check/requirements.txt`
- Create: `tools/health-check/requirements-dev.txt`
- Create: `tools/health-check/README.md`

**Interfaces:**
- Consumes: `tools/health-check/run_check.py`'s `main()` entry point and its required env vars (`Config.from_env`).
- Produces: nothing consumed by other tasks — this is the final integration artifact.

- [ ] **Step 1: Create the runtime and dev dependency files**

Create `tools/health-check/requirements.txt`:

```
requests==2.32.3
```

Create `tools/health-check/requirements-dev.txt`:

```
-r requirements.txt
pytest==8.3.3
```

- [ ] **Step 2: Create the GitHub Actions workflow**

Create `.github/workflows/daily-health-check.yml`:

```yaml
name: Daily Health Check

on:
  schedule:
    - cron: "35 22 * * *"
  workflow_dispatch: {}

jobs:
  health-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Set up Python
        uses: actions/setup-python@v5
        with:
          python-version: "3.12"

      - name: Install dependencies
        run: pip install -r tools/health-check/requirements.txt

      - name: Run health check
        env:
          BASE_URL: https://microplex.lk
          QA_SMS_API_KEY: ${{ secrets.QA_SMS_API_KEY }}
          QA_EMAIL_API_KEY: ${{ secrets.QA_EMAIL_API_KEY }}
          QA_TEST_PHONE: ${{ secrets.QA_TEST_PHONE }}
          QA_TEST_EMAIL: ${{ secrets.QA_TEST_EMAIL }}
          QA_SMS_SENDER_ID: ${{ secrets.QA_SMS_SENDER_ID }}
          BREVO_API_KEY: ${{ secrets.BREVO_API_KEY }}
          REPORT_RECIPIENT: ${{ secrets.REPORT_RECIPIENT }}
        run: python tools/health-check/run_check.py
```

- [ ] **Step 3: Write the setup README**

Create `tools/health-check/README.md`:

```markdown
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

\`\`\`bash
gh workflow run daily-health-check.yml
\`\`\`
```

- [ ] **Step 4: Run the full test suite one final time**

Run: `cd tools/health-check && python -m pytest tests/ -v`
Expected: PASS (49 tests) — confirms nothing in this task's file additions broke the script.

- [ ] **Step 5: Manually verify the workflow file**

Re-read `.github/workflows/daily-health-check.yml` and confirm:
- The cron value is exactly `"35 22 * * *"` (matches the Global Constraints section).
- All seven secrets from the README's setup table are referenced under `env:`.
- `workflow_dispatch: {}` is present so the workflow can be run manually before the first scheduled fire.

- [ ] **Step 6: Commit**

```bash
git add .github/workflows/daily-health-check.yml tools/health-check/requirements.txt tools/health-check/requirements-dev.txt tools/health-check/README.md
git commit -m "Add daily health check GitHub Actions workflow and setup docs"
```

---

## After implementation (manual, not part of the automated task run)

These require the user's own action in GitHub/the admin panel and cannot be done by an engineer working only in this checkout:

1. Create the QA test `Client` in the Microplex admin panel and generate its API keys (README Step 1-2).
2. Add all seven secrets listed in the README to the repository.
3. Trigger the workflow manually once (`gh workflow run daily-health-check.yml` or the Actions tab "Run workflow" button) and confirm the report email arrives correctly at `contacts.nmssalman@gmail.com`.
4. Confirm the first scheduled run fires the next morning as expected.
