namespace Microplex.Web.Services;

public static class MonitoredApiEndpoints
{
    public static readonly (string ApiName, string MethodName, string Endpoint)[] All =
    [
        ("SMS Gateway", "Send", "POST /api/sms/send"),
        ("SMS Gateway", "Balance", "GET /api/sms/balance"),
        ("Email Gateway", "Send", "POST /api/email/send"),
        ("Email Gateway", "Balance", "GET /api/email/balance"),
    ];
}
