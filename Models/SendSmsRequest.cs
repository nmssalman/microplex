using System.Text.Json.Serialization;

namespace Microplex.Web.Models;

public sealed class SendSmsRequest
{
    [JsonPropertyName("api_token")]
    public string? ApiToken { get; set; }

    [JsonPropertyName("recipient")]
    public string? Recipient { get; set; }

    [JsonPropertyName("sender_id")]
    public string? SenderId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
