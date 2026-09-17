using Microplex.Web.Data;

namespace Microplex.Web.Models;

public sealed class EmailInsightViewModel
{
    public IReadOnlyList<Client> Companies { get; set; } = [];
    public Guid? SelectedClientId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public IReadOnlyList<EmailMessageLog> Logs { get; set; } = [];
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
}
