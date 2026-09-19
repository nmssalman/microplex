using System.ComponentModel.DataAnnotations;

namespace Microplex.Web.Models;

public sealed class AcademyEnrollmentRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [RegularExpression(@"^\+?[0-9 ()-]{7,20}$", ErrorMessage = "Enter a valid contact number.")]
    public string ContactNo { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Program { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
