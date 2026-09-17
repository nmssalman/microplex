using System.Text.Json.Serialization;

namespace Microplex.Web.Models;

public sealed class SendEmailRequest
{
    [JsonPropertyName("api_token")]
    public string? ApiToken { get; set; }

    [JsonPropertyName("recipient")]
    public string? Recipient { get; set; }

    [JsonPropertyName("recipient_name")]
    public string? RecipientName { get; set; }

    [JsonPropertyName("sender_name")]
    public string? SenderName { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
