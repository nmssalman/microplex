using Microplex.Web.Data;

namespace Microplex.Web.Models;

public sealed class ApiStatusItemViewModel
{
    public string ApiName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public ApiHealthStatus Status { get; set; } = ApiHealthStatus.Operational;
    public int ConsecutiveFailures { get; set; }
    public DateTime? LastCheckedAtUtc { get; set; }
    public DateTime? LastSuccessAtUtc { get; set; }
}

public sealed class StatusPageViewModel
{
    public List<ApiStatusItemViewModel> Items { get; set; } = [];
    public ApiHealthStatus OverallStatus { get; set; } = ApiHealthStatus.Operational;
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
