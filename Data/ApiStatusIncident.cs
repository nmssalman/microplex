using System.ComponentModel.DataAnnotations;

namespace Microplex.Web.Data;

public sealed class ApiStatusIncident
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(80)] public string ApiName { get; set; } = string.Empty;
    [Required, StringLength(80)] public string MethodName { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    [Required, StringLength(1000)] public string ErrorMessage { get; set; } = string.Empty;
}
