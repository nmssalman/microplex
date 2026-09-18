using System.Net.Http.Json;

namespace Microplex.Web.Services;

public sealed record InternalEmailSendResult(bool Success, int StatusCode, string ResponseBody);

public sealed class InternalEmailApiClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<InternalEmailSendResult> SendAsync(string recipient, string? recipientName, string subject, string htmlMessage, string? senderName = null, CancellationToken cancellationToken = default)
    {
        var apiToken = configuration["InternalEmailApi:ApiToken"];
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new InvalidOperationException("Internal email sending is not configured. Set InternalEmailApi:ApiToken (user secrets or environment variable).");

        var baseUrl = configuration["InternalEmailApi:BaseUrl"] ?? "https://microplex.lk/api/email/send";

        var payload = new
        {
            api_token = apiToken,
            recipient,
            recipient_name = recipientName,
            sender_name = senderName,
            subject,
            message = htmlMessage
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl) { Content = JsonContent.Create(payload) };
        request.Headers.TryAddWithoutValidation("api_token", apiToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new InternalEmailSendResult(response.IsSuccessStatusCode, (int)response.StatusCode, body);
    }

    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync(toAddress, null, subject, htmlBody, null, cancellationToken);
        if (!result.Success)
            throw new InvalidOperationException($"Microplex Email API rejected the email (HTTP {result.StatusCode}): {result.ResponseBody}");
    }
}
