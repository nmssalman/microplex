using Microplex.Web.Data;

namespace Microplex.Web.Models;

public sealed class DashboardViewModel
{
    public int TotalClients { get; set; }
    public IReadOnlyList<Client> RecentClients { get; set; } = Array.Empty<Client>();
}
