#!/usr/bin/env python3
"""Daily production health check for microplex.lk.

See docs/superpowers/specs/2026-09-19-daily-health-check-design.md.
"""
from __future__ import annotations

import html as html_module
import os
import re
import time
from dataclasses import dataclass
from datetime import datetime
from typing import Optional
from zoneinfo import ZoneInfo

import requests

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
