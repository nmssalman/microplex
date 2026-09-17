using System.ComponentModel.DataAnnotations;

namespace Microplex.Web.Data;

public sealed class Client
{
    [Key] public Guid ClientId { get; set; } = Guid.NewGuid();

    [Required, StringLength(200), Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(16)]
    [RegularExpression(@"^\+[1-9]\d{7,14}$", ErrorMessage = "Use international format, for example +94711111344.")]
    public string Mobile { get; set; } = string.Empty;

    [Url, StringLength(300)] public string? Website { get; set; }
    [Display(Name = "Software Solution")] public bool UsesSoftwareSolution { get; set; }
    [Display(Name = "Email Solution")] public bool UsesEmailSolution { get; set; }
    [Display(Name = "SMS Solution")] public bool UsesSmsSolution { get; set; }
    [Display(Name = "SMS Credits")] public int SmsCredits { get; set; }
    [StringLength(128), Display(Name = "SMS API Key")] public string? SmsApiKey { get; set; }
    [Display(Name = "Email Credits")] public int EmailCredits { get; set; }
    [StringLength(128), Display(Name = "Email API Key")] public string? EmailApiKey { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
