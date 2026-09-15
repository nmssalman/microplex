using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microplex.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Client>(entity =>
        {
            entity.HasKey(x => x.ClientId);
            entity.Property(x => x.ClientId).ValueGeneratedNever();
            entity.HasIndex(x => x.CompanyName);
            entity.HasIndex(x => x.Email);
            entity.HasIndex(x => x.SmsApiKey).IsUnique();
        });
    }
}
