#!/usr/bin/env python3
"""Daily production health check for microplex.lk.

See docs/superpowers/specs/2026-09-19-daily-health-check-design.md.
"""
from __future__ import annotations

import os
import time
from dataclasses import dataclass
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
