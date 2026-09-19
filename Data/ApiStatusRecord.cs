using System.ComponentModel.DataAnnotations;

namespace Microplex.Web.Data;

public enum ApiHealthStatus
{
    Operational,
    Warning,
    Stopped
}

public sealed class ApiStatusRecord
{
    [Key] public int Id { get; set; }

    [Required, StringLength(80)] public string ApiName { get; set; } = string.Empty;
    [Required, StringLength(80)] public string MethodName { get; set; } = string.Empty;

    public ApiHealthStatus Status { get; set; } = ApiHealthStatus.Operational;
    public int ConsecutiveFailures { get; set; }

    public DateTime? LastCheckedAtUtc { get; set; }
    public DateTime? LastSuccessAtUtc { get; set; }
    public DateTime? LastFailureAtUtc { get; set; }
    [StringLength(500)] public string? LastErrorMessage { get; set; }
}
