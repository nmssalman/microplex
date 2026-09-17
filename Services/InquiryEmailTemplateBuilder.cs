using System.Net;

namespace Microplex.Web.Services;

public static class InquiryEmailTemplateBuilder
{
    public static string BuildThankYou(string service)
    {
        var safeService = WebUtility.HtmlEncode(service);
        return ThankYouTemplate.Replace("{{Service}}", safeService);
    }

    public static string BuildNotification(string inquirerEmail, string service)
    {
        var safeEmail = WebUtility.HtmlEncode(inquirerEmail);
        var safeService = WebUtility.HtmlEncode(service);
        return NotificationTemplate
            .Replace("{{InquirerEmail}}", safeEmail)
            .Replace("{{Service}}", safeService);
    }

    private const string ThankYouTemplate = """
    <!DOCTYPE html>
    <html lang="en">
    <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Thank you for contacting Microplex</title>
    </head>
    <body style="margin:0;padding:0;background:#f5f8fc;font-family:Arial,Helvetica,sans-serif;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f5f8fc;padding:40px 0;">
        <tr><td align="center">
          <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 20px 50px rgba(13,39,80,.12);max-width:92%;">
            <tr><td style="background:#07162f;padding:26px 32px;">
              <table role="presentation"><tr>
                <td style="width:36px;"><div style="width:36px;height:36px;border-radius:10px;background:linear-gradient(135deg,#15803d,#19d3e0);color:#fff;font-weight:900;font-size:18px;text-align:center;line-height:36px;">M</div></td>
                <td style="color:#ffffff;font-weight:800;letter-spacing:.12em;font-size:14px;padding-left:12px;">MICROPLEX</td>
              </tr></table>
            </td></tr>
            <tr><td style="padding:40px 32px 8px;text-align:center;">
              <div style="width:64px;height:64px;border-radius:50%;background:#e8f8f0;color:#16925c;font-size:30px;line-height:64px;margin:0 auto 20px;">&#10003;</div>
              <h1 style="margin:0 0 8px;font-size:22px;color:#15213a;">Thank you for contacting us</h1>
            </td></tr>
            <tr><td style="padding:0 32px 32px;color:#15213a;font-size:15px;line-height:1.8;">
              <p style="margin:0 0 16px;">Hello,</p>
              <p style="margin:0 0 16px;">Thank you for your interest in our <strong>{{Service}}</strong>. Our team has received your inquiry and will get back to you shortly to help you get integrated.</p>
              <p style="margin:0;">Best Regards,<br />Microplex Corporation</p>
            </td></tr>
            <tr><td style="background:#050f22;padding:20px 32px;text-align:center;color:#8798b3;font-size:12px;">&copy; 2026 Microplex Corporation</td></tr>
          </table>
        </td></tr>
      </table>
    </body>
    </html>
    """;

    private const string NotificationTemplate = """
    <!DOCTYPE html>
    <html lang="en">
    <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>New integration inquiry</title>
    </head>
    <body style="margin:0;padding:0;background:#f5f8fc;font-family:Arial,Helvetica,sans-serif;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f5f8fc;padding:40px 0;">
        <tr><td align="center">
          <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 20px 50px rgba(13,39,80,.12);max-width:92%;">
            <tr><td style="background:#07162f;padding:26px 32px;">
              <table role="presentation"><tr>
                <td style="width:36px;"><div style="width:36px;height:36px;border-radius:10px;background:linear-gradient(135deg,#15803d,#19d3e0);color:#fff;font-weight:900;font-size:18px;text-align:center;line-height:36px;">M</div></td>
                <td style="color:#ffffff;font-weight:800;letter-spacing:.12em;font-size:14px;padding-left:12px;">MICROPLEX</td>
              </tr></table>
            </td></tr>
            <tr><td style="padding:40px 32px 8px;text-align:center;">
              <h1 style="margin:0 0 8px;font-size:22px;color:#15213a;">New Integration Inquiry</h1>
            </td></tr>
            <tr><td style="padding:0 32px 32px;color:#15213a;font-size:15px;line-height:1.8;">
              <p style="margin:0 0 16px;">A visitor requested integration details from the public Solutions page.</p>
              <p style="margin:0 0 8px;"><strong>Service:</strong> {{Service}}</p>
              <p style="margin:0;"><strong>Email:</strong> <a href="mailto:{{InquirerEmail}}">{{InquirerEmail}}</a></p>
            </td></tr>
            <tr><td style="background:#050f22;padding:20px 32px;text-align:center;color:#8798b3;font-size:12px;">&copy; 2026 Microplex Corporation</td></tr>
          </table>
        </td></tr>
      </table>
    </body>
    </html>
    """;
}
