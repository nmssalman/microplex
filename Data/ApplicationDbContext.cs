using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microplex.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<SmsMessageLog> SmsMessageLogs => Set<SmsMessageLog>();

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
        builder.Entity<SmsMessageLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => x.SentAtUtc);
            entity.HasIndex(x => x.ClientId);
            entity.HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
