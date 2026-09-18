from unittest.mock import MagicMock

import pytest
import requests

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
