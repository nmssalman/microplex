using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Microplex.Web.Services;

public sealed record EmailSendResult(bool Success, int StatusCode, string ResponseBody);

public sealed class EmailSender(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<EmailSendResult> SendAsync(string toAddress, string? toName, string subject, string htmlBody, string? senderNameOverride = null, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["Brevo:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Email is not configured. Set Brevo:ApiKey (user secrets or environment variable) and Brevo:SenderEmail.");

        var senderEmail = configuration["Brevo:SenderEmail"] ?? "info@microplex.lk";
        var senderName = string.IsNullOrWhiteSpace(senderNameOverride)
            ? (configuration["Brevo:SenderName"] ?? "Microplex Corporation")
            : senderNameOverride;

        var payload = new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { email = toAddress, name = toName } },
            subject,
            htmlContent = htmlBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new EmailSendResult(response.IsSuccessStatusCode, (int)response.StatusCode, body);
    }

    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync(toAddress, null, subject, htmlBody, null, cancellationToken);
        if (!result.Success)
            throw new InvalidOperationException($"Brevo rejected the email (HTTP {result.StatusCode}): {result.ResponseBody}");
    }
}
