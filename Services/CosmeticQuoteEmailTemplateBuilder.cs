using System.Net;

namespace Microplex.Web.Services;

public static class CosmeticQuoteEmailTemplateBuilder
{
    public static string BuildThankYou(string itemName)
    {
        var safeItem = WebUtility.HtmlEncode(itemName);
        return ThankYouTemplate.Replace("{{ItemName}}", safeItem);
    }

    public static string BuildNotification(string name, string whatsAppContact, string? email, string address, string country, string itemName)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeWhatsApp = WebUtility.HtmlEncode(whatsAppContact);
        var safeEmail = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(email) ? "Not provided" : email);
        var safeAddress = WebUtility.HtmlEncode(address);
        var safeCountry = WebUtility.HtmlEncode(country);
        var safeItem = WebUtility.HtmlEncode(itemName);
        return NotificationTemplate
            .Replace("{{Name}}", safeName)
            .Replace("{{WhatsAppContact}}", safeWhatsApp)
            .Replace("{{Email}}", safeEmail)
            .Replace("{{Address}}", safeAddress)
            .Replace("{{Country}}", safeCountry)
            .Replace("{{ItemName}}", safeItem);
    }

    private const string ThankYouTemplate = """
    <!DOCTYPE html>
    <html lang="en">
    <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Your quotation request &mdash; Microplex Cosmetics</title>
    </head>
    <body style="margin:0;padding:0;background:#faf3f0;font-family:Arial,Helvetica,sans-serif;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#faf3f0;padding:40px 0;">
        <tr><td align="center">
          <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 20px 50px rgba(120,70,70,.12);max-width:92%;">
            <tr><td style="background:#3a2620;padding:26px 32px;">
              <table role="presentation"><tr>
                <td style="width:36px;"><div style="width:36px;height:36px;border-radius:10px;background:linear-gradient(135deg,#c9a7a0,#e0b84f);color:#fff;font-weight:900;font-size:18px;text-align:center;line-height:36px;">M</div></td>
                <td style="color:#ffffff;font-weight:800;letter-spacing:.12em;font-size:14px;padding-left:12px;">MICROPLEX COSMETICS</td>
              </tr></table>
            </td></tr>
            <tr><td style="padding:40px 32px 8px;text-align:center;">
              <div style="width:64px;height:64px;border-radius:50%;background:#f6ece7;color:#a86a52;font-size:30px;line-height:64px;margin:0 auto 20px;">&#10003;</div>
              <h1 style="margin:0 0 8px;font-size:22px;color:#3a2620;">We've received your quotation request</h1>
            </td></tr>
            <tr><td style="padding:0 32px 32px;color:#3a2620;font-size:15px;line-height:1.8;">
              <p style="margin:0 0 16px;">Hello,</p>
              <p style="margin:0 0 16px;">Thank you for your interest in <strong>{{ItemName}}</strong>. We'll prepare your quotation, including shipping to your country, and send it to you directly on WhatsApp shortly.</p>
              <p style="margin:0;">Best Regards,<br />Microplex Cosmetics</p>
            </td></tr>
            <tr><td style="background:#2a1a15;padding:20px 32px;text-align:center;color:#c9b5ad;font-size:12px;">&copy; 2026 Microplex Corporation</td></tr>
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
    <title>New cosmetics quotation request</title>
    </head>
    <body style="margin:0;padding:0;background:#faf3f0;font-family:Arial,Helvetica,sans-serif;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#faf3f0;padding:40px 0;">
        <tr><td align="center">
          <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 20px 50px rgba(120,70,70,.12);max-width:92%;">
            <tr><td style="background:#3a2620;padding:26px 32px;">
              <table role="presentation"><tr>
                <td style="width:36px;"><div style="width:36px;height:36px;border-radius:10px;background:linear-gradient(135deg,#c9a7a0,#e0b84f);color:#fff;font-weight:900;font-size:18px;text-align:center;line-height:36px;">M</div></td>
                <td style="color:#ffffff;font-weight:800;letter-spacing:.12em;font-size:14px;padding-left:12px;">MICROPLEX COSMETICS</td>
              </tr></table>
            </td></tr>
            <tr><td style="padding:40px 32px 8px;text-align:center;">
              <h1 style="margin:0 0 8px;font-size:22px;color:#3a2620;">New Quotation Request</h1>
            </td></tr>
            <tr><td style="padding:0 32px 32px;color:#3a2620;font-size:15px;line-height:1.8;">
              <p style="margin:0 0 16px;">A visitor requested a quotation from the public Cosmetics page. Please prepare the quote and send it via WhatsApp.</p>
              <p style="margin:0 0 8px;"><strong>Item:</strong> {{ItemName}}</p>
              <p style="margin:0 0 8px;"><strong>Name:</strong> {{Name}}</p>
              <p style="margin:0 0 8px;"><strong>WhatsApp:</strong> {{WhatsAppContact}}</p>
              <p style="margin:0 0 8px;"><strong>Email:</strong> {{Email}}</p>
              <p style="margin:0 0 8px;"><strong>Address:</strong> {{Address}}</p>
              <p style="margin:0;"><strong>Country:</strong> {{Country}}</p>
            </td></tr>
            <tr><td style="background:#2a1a15;padding:20px 32px;text-align:center;color:#c9b5ad;font-size:12px;">&copy; 2026 Microplex Corporation</td></tr>
          </table>
        </td></tr>
      </table>
    </body>
    </html>
    """;
}
