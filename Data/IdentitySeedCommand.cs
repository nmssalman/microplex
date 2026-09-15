using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Microplex.Web.Data;

public static class IdentitySeedCommand
{
    public static async Task RunAsync(IConfiguration configuration)
    {
        var connectionString = DatabaseConfiguration.GetRequiredConnectionString(configuration);
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options);
        const string roleName = IdentitySeeder.SuperAdminRole;
        var normalizedRole = roleName.ToUpperInvariant();
        var role = await db.Roles.SingleOrDefaultAsync(r => r.NormalizedName == normalizedRole);
        if (role is null)
        {
            role = new IdentityRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = roleName,
                NormalizedName = normalizedRole,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            db.Roles.Add(role);
        }

        var username = Required(configuration, "SuperAdminSeed:Username");
        var normalizedUsername = username.ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = username,
                NormalizedUserName = normalizedUsername,
                Email = Required(configuration, "SuperAdminSeed:Email"),
                FullName = Required(configuration, "SuperAdminSeed:FullName"),
                PhoneNumber = Required(configuration, "SuperAdminSeed:PhoneNumber"),
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            user.NormalizedEmail = user.Email.ToUpperInvariant();
            user.PasswordHash = new PasswordHasher<ApplicationUser>()
                .HashPassword(user, Required(configuration, "SuperAdminSeed:Password"));
            db.Users.Add(user);
        }

        await db.SaveChangesAsync();
        if (!await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id))
        {
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
        }

        Console.WriteLine($"Super administrator '{username}' is ready.");
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Secret '{key}' is not configured.");
}
