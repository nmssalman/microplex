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


def test_check_inquiry_email_submit_http_error_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    get_response = MagicMock(status_code=200, text=TOKEN_HTML)
    post_response = MagicMock(status_code=400, text="Bad Request")
    session.request.side_effect = [get_response, post_response]

    results = rc.check_inquiry_email(config, session)

    assert results[0].status == rc.FAIL
    assert "400" in results[0].detail


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


def test_check_sms_balance_exception_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = requests.Timeout("timed out")

    results = rc.check_sms_balance(config, session)

    assert results[0].status == rc.FAIL
    assert "timed out" in results[0].detail


def test_check_sms_balance_missing_balance_field_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200, json=lambda: {"success": True})

    results = rc.check_sms_balance(config, session)

    assert results[0].status == rc.FAIL
