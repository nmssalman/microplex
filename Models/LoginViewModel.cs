using System.ComponentModel.DataAnnotations;

namespace Microplex.Web.Models;

public sealed class LoginViewModel
{
    [Required, Display(Name = "Email or username")]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }
}
