using Microsoft.AspNetCore.Identity;

namespace Microplex.Web.Data;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
