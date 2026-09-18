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
