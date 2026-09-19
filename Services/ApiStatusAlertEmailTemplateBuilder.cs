using System.Net;

namespace Microplex.Web.Services;

public static class ApiStatusAlertEmailTemplateBuilder
{
    public static string Build(string apiName, string methodName, int consecutiveFailures, string? lastErrorMessage)
    {
        var safeApiName = WebUtility.HtmlEncode(apiName);
        var safeMethodName = WebUtility.HtmlEncode(methodName);
        var safeError = WebUtility.HtmlEncode(lastErrorMessage ?? "No further details were captured.");

        return Template
            .Replace("{{ApiName}}", safeApiName)
            .Replace("{{MethodName}}", safeMethodName)
            .Replace("{{Failures}}", consecutiveFailures.ToString())
            .Replace("{{LastError}}", safeError);
    }

    private const string Template = """
    <!DOCTYPE html>
    <html lang="en">
    <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Urgent: API Status Alert</title>
    <style>
      @keyframes pulse-warn { 0%,100% { transform: scale(1); } 50% { transform: scale(1.08); } }
      @keyframes pulse-ring {
        0% { box-shadow: 0 0 0 0 rgba(220,38,38,.45); }
        70% { box-shadow: 0 0 0 14px rgba(220,38,38,0); }
        100% { box-shadow: 0 0 0 0 rgba(220,38,38,0); }
      }
      .alert-icon { animation: pulse-warn 1.6s ease-in-out infinite, pulse-ring 1.6s ease-in-out infinite; }
    </style>
    </head>
    <body style="margin:0;padding:0;background:#f5f8fc;font-family:Arial,Helvetica,sans-serif;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f5f8fc;padding:40px 0;">
        <tr><td align="center">
          <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 20px 50px rgba(13,39,80,.12);max-width:92%;">
            <tr><td style="background:#07162f;padding:26px 32px;">
              <table role="presentation"><tr>
                <td style="width:36px;"><div style="width:36px;height:36px;border-radius:10px;background:linear-gradient(135deg,#dc2626,#7c3aed);color:#fff;font-weight:900;font-size:18px;text-align:center;line-height:36px;">M</div></td>
                <td style="color:#ffffff;font-weight:800;letter-spacing:.12em;font-size:14px;padding-left:12px;">MICROPLEX</td>
              </tr></table>
            </td></tr>
            <tr><td style="padding:40px 32px 8px;text-align:center;">
              <div class="alert-icon" style="width:64px;height:64px;border-radius:50%;background:#fdecec;color:#dc2626;font-size:30px;line-height:64px;margin:0 auto 20px;">&#9888;</div>
              <h1 style="margin:0 0 8px;font-size:22px;color:#15213a;">API Status: Stopped</h1>
            </td></tr>
            <tr><td style="padding:0 32px;text-align:center;">
              <div style="display:inline-block;padding:14px 28px;border-radius:14px;background:#fdecec;margin:8px 0 24px;">
                <div style="font-size:11px;font-weight:800;letter-spacing:.12em;color:#dc2626;">{{ApiName}} &middot; {{MethodName}}</div>
                <div style="font-size:28px;font-weight:900;color:#dc2626;margin-top:4px;">{{Failures}} failures in a row</div>
              </div>
            </td></tr>
            <tr><td style="padding:0 32px 32px;color:#15213a;font-size:15px;line-height:1.8;">
              <p style="margin:0 0 16px;">This endpoint has failed more than 3 times consecutively and has been marked <strong>Stopped</strong> on the public status page.</p>
              <p style="margin:0 0 16px;padding:12px 16px;background:#f5f8fc;border-radius:10px;font-family:monospace;font-size:13px;color:#66738a;">{{LastError}}</p>
              <p style="margin:0;">Please investigate as soon as possible.<br />&mdash; Microplex API Monitor</p>
            </td></tr>
            <tr><td style="background:#050f22;padding:20px 32px;text-align:center;color:#8798b3;font-size:12px;">&copy; 2026 Microplex Corporation</td></tr>
          </table>
        </td></tr>
      </table>
    </body>
    </html>
    """;
}
