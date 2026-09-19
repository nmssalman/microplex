using System.Net;

namespace Microplex.Web.Services;

public static class AcademyEnrollmentEmailTemplateBuilder
{
    public static string BuildThankYou(string program)
    {
        var safeProgram = WebUtility.HtmlEncode(program);
        return ThankYouTemplate.Replace("{{Program}}", safeProgram);
    }

    public static string BuildNotification(string name, string email, string contactNo, string program)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeEmail = WebUtility.HtmlEncode(email);
        var safeContactNo = WebUtility.HtmlEncode(contactNo);
        var safeProgram = WebUtility.HtmlEncode(program);
        return NotificationTemplate
            .Replace("{{Name}}", safeName)
            .Replace("{{Email}}", safeEmail)
            .Replace("{{ContactNo}}", safeContactNo)
            .Replace("{{Program}}", safeProgram);
    }

    private const string ThankYouTemplate = """
    <!DOCTYPE html>
    <html lang="en">
    <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Thank you for enrolling interest &mdash; Microplex Academy</title>
    </head>
    <body style="margin:0;padding:0;background:#f5f8fc;font-family:Arial,Helvetica,sans-serif;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f5f8fc;padding:40px 0;">
        <tr><td align="center">
          <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 20px 50px rgba(13,39,80,.12);max-width:92%;">
            <tr><td style="background:#07162f;padding:26px 32px;">
              <table role="presentation"><tr>
                <td style="width:36px;"><div style="width:36px;height:36px;border-radius:10px;background:linear-gradient(135deg,#15803d,#19d3e0);color:#fff;font-weight:900;font-size:18px;text-align:center;line-height:36px;">M</div></td>
                <td style="color:#ffffff;font-weight:800;letter-spacing:.12em;font-size:14px;padding-left:12px;">MICROPLEX ACADEMY</td>
              </tr></table>
            </td></tr>
            <tr><td style="padding:40px 32px 8px;text-align:center;">
              <div style="width:64px;height:64px;border-radius:50%;background:#e8f8f0;color:#16925c;font-size:30px;line-height:64px;margin:0 auto 20px;">&#10003;</div>
              <h1 style="margin:0 0 8px;font-size:22px;color:#15213a;">We've received your enrollment interest</h1>
            </td></tr>
            <tr><td style="padding:0 32px 32px;color:#15213a;font-size:15px;line-height:1.8;">
              <p style="margin:0 0 16px;">Hello,</p>
              <p style="margin:0 0 16px;">Thank you for your interest in our <strong>{{Program}}</strong> program at Microplex Academy. Our team will contact you shortly with the next steps.</p>
              <p style="margin:0;">Best Regards,<br />Microplex Academy</p>
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
    <title>New Academy enrollment</title>
    </head>
    <body style="margin:0;padding:0;background:#f5f8fc;font-family:Arial,Helvetica,sans-serif;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f5f8fc;padding:40px 0;">
        <tr><td align="center">
          <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 20px 50px rgba(13,39,80,.12);max-width:92%;">
            <tr><td style="background:#07162f;padding:26px 32px;">
              <table role="presentation"><tr>
                <td style="width:36px;"><div style="width:36px;height:36px;border-radius:10px;background:linear-gradient(135deg,#15803d,#19d3e0);color:#fff;font-weight:900;font-size:18px;text-align:center;line-height:36px;">M</div></td>
                <td style="color:#ffffff;font-weight:800;letter-spacing:.12em;font-size:14px;padding-left:12px;">MICROPLEX ACADEMY</td>
              </tr></table>
            </td></tr>
            <tr><td style="padding:40px 32px 8px;text-align:center;">
              <h1 style="margin:0 0 8px;font-size:22px;color:#15213a;">New Academy Enrollment</h1>
            </td></tr>
            <tr><td style="padding:0 32px 32px;color:#15213a;font-size:15px;line-height:1.8;">
              <p style="margin:0 0 16px;">A visitor requested enrollment from the public Academy page.</p>
              <p style="margin:0 0 8px;"><strong>Program:</strong> {{Program}}</p>
              <p style="margin:0 0 8px;"><strong>Name:</strong> {{Name}}</p>
              <p style="margin:0 0 8px;"><strong>Email:</strong> <a href="mailto:{{Email}}">{{Email}}</a></p>
              <p style="margin:0;"><strong>Contact No:</strong> {{ContactNo}}</p>
            </td></tr>
            <tr><td style="background:#050f22;padding:20px 32px;text-align:center;color:#8798b3;font-size:12px;">&copy; 2026 Microplex Corporation</td></tr>
          </table>
        </td></tr>
      </table>
    </body>
    </html>
    """;
}
