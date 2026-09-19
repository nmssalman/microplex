using Microplex.Web.Data;

namespace Microplex.Web.Models;

public sealed class ManagementItemViewModel
{
    public int? RecordId { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public ApiHealthStatus Status { get; set; } = ApiHealthStatus.Operational;
    public int ConsecutiveFailures { get; set; }
    public DateTime? LastCheckedAtUtc { get; set; }
    public DateTime? LastFailureAtUtc { get; set; }
    public string? LastErrorMessage { get; set; }
}

public sealed class ReportsViewModel
{
    public List<ApiStatusIncident> Incidents { get; set; } = [];
    public string? SelectedApiName { get; set; }
    public List<string> ApiNames { get; set; } = [];
}
