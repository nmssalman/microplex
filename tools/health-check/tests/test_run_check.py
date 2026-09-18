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


def test_check_email_send_exception_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = requests.Timeout("timed out")

    results = rc.check_email_send(config, session)

    assert results[0].status == rc.FAIL
    assert "timed out" in results[0].detail


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


def test_check_email_balance_exception_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = requests.Timeout("timed out")

    results = rc.check_email_balance(config, session)

    assert results[0].status == rc.FAIL
    assert "timed out" in results[0].detail


def test_check_email_balance_missing_balance_field_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.return_value = MagicMock(status_code=200, json=lambda: {"success": True})

    results = rc.check_email_balance(config, session)

    assert results[0].status == rc.FAIL


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


def test_check_other_features_exception_on_any_request_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = requests.Timeout("timed out")

    results = rc.check_other_features(config, session)

    assert all(r.status == rc.FAIL for r in results)
    assert all("timed out" in r.detail for r in results)


def test_check_other_features_unexpected_status_on_protected_path_is_fail():
    config = make_config()
    session = MagicMock(spec=requests.Session)
    session.request.side_effect = _other_features_fake_request(protected_status=403, protected_location="")

    results = rc.check_other_features(config, session)

    gate_results = [r for r in results if r.name.startswith("Auth gate")]
    assert all(r.status == rc.FAIL for r in gate_results)


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


def test_render_html_escapes_html_in_result_fields():
    from datetime import datetime as dt

    results = [rc.Result("Cat & Co", "<script>alert(1)</script>", rc.FAIL, "Tom & Jerry", 5)]

    output = rc.render_html(results, dt(2026, 9, 19, 4, 5), "")

    assert "<script>" not in output
    assert "&lt;script&gt;" in output
    assert "Tom &amp; Jerry" in output
    assert "Cat &amp; Co" in output
    assert "Tom & Jerry" not in output
    assert "Cat & Co" not in output
