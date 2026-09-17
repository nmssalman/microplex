using System.ComponentModel.DataAnnotations;

namespace Microplex.Web.Data;

public sealed class SmsMessageLog
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }

    [Required, StringLength(20)] public string Recipient { get; set; } = string.Empty;
    [Required, StringLength(20)] public string SenderId { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Type { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string Message { get; set; } = string.Empty;

    public bool Success { get; set; }
    [StringLength(500)] public string? FailureReason { get; set; }
    public string? GatewayResponse { get; set; }
    public int? BalanceAfter { get; set; }

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
