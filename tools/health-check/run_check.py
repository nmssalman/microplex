#!/usr/bin/env python3
"""Daily production health check for microplex.lk.

See docs/superpowers/specs/2026-09-19-daily-health-check-design.md.
"""
from __future__ import annotations

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
