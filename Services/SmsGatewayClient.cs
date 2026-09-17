using System.Net.Http.Json;

namespace Microplex.Web.Services;

public sealed record SmsGatewayResult(bool Success, int StatusCode, string ResponseBody);

public sealed class SmsGatewayClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<SmsGatewayResult> SendAsync(string recipient, string senderId, string type, string message, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["SmsGateway:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("SmsGateway:BaseUrl is not configured.");

        var payload = new
        {
            api_token = ResolveApiToken(),
            recipient,
            sender_id = senderId,
            type,
            message
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync(baseUrl, payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new SmsGatewayResult(response.IsSuccessStatusCode, (int)response.StatusCode, body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new SmsGatewayResult(false, 0, ex.Message);
        }
    }

    private string ResolveApiToken()
    {
        var token = configuration["SmsGateway:ApiToken"];
        if (string.IsNullOrWhiteSpace(token))
            token = Environment.GetEnvironmentVariable("SMS_GATEWAY_API_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("SMS gateway API token is not configured. Set SmsGateway:ApiToken (user secrets) or the SMS_GATEWAY_API_TOKEN environment variable.");

        return token;
    }
}
