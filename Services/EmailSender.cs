using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Microplex.Web.Services;

public sealed class EmailSender(HttpClient httpClient, IConfiguration configuration)
{
    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["Brevo:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Email is not configured. Set Brevo:ApiKey (user secrets or environment variable) and Brevo:SenderEmail.");

        var senderEmail = configuration["Brevo:SenderEmail"] ?? "info@microplex.lk";
        var senderName = configuration["Brevo:SenderName"] ?? "Microplex Corporation";

        var payload = new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { email = toAddress } },
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
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Brevo rejected the email (HTTP {(int)response.StatusCode}): {body}");
        }
    }
}
