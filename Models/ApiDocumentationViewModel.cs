using Microplex.Web.Data;

namespace Microplex.Web.Models;

public sealed class ApiDocumentationViewModel
{
    public IReadOnlyList<Client> Clients { get; set; } = [];
    public Guid? SelectedClientId { get; set; }
    public Client? SelectedClient { get; set; }
}
