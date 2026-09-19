using System.ComponentModel.DataAnnotations;

namespace Microplex.Web.Models;

public sealed class CosmeticQuoteRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [RegularExpression(@"^\+?[0-9 ()-]{7,20}$", ErrorMessage = "Enter a valid WhatsApp number.")]
    public string WhatsAppContact { get; set; } = string.Empty;

    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string ItemName { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
