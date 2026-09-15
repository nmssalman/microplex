using Microsoft.AspNetCore.Identity;

namespace Microplex.Web.Data;

public static class IdentitySeeder
{
    public const string SuperAdminRole = "SuperAdmin";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(SuperAdminRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(SuperAdminRole));
            EnsureSucceeded(roleResult, "create the SuperAdmin role");
        }

        var username = configuration["SuperAdminSeed:Username"];
        var password = configuration["SuperAdminSeed:Password"];
        var email = configuration["SuperAdminSeed:Email"];
        var fullName = configuration["SuperAdminSeed:FullName"];
        var phoneNumber = configuration["SuperAdminSeed:PhoneNumber"];
        if (new[] { username, password, email, fullName, phoneNumber }.Any(string.IsNullOrWhiteSpace))
            return;

        var user = await userManager.FindByNameAsync(username!);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = fullName!,
                PhoneNumber = phoneNumber,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            EnsureSucceeded(await userManager.CreateAsync(user, password!), "create the initial super administrator");
        }

        if (!await userManager.IsInRoleAsync(user, SuperAdminRole))
            EnsureSucceeded(await userManager.AddToRoleAsync(user, SuperAdminRole), "assign the SuperAdmin role");

    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException($"Unable to {action}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
