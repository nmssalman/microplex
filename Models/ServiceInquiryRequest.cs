using System.ComponentModel.DataAnnotations;

namespace Microplex.Web.Models;

public sealed class ServiceInquiryRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Service { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
