using Microsoft.EntityFrameworkCore;
using Saas.OrganisationDomain.Api.Auth.Domain;

namespace Saas.OrganisationDomain.Api.Persistence;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.Property(k => k.OrganisationReference).IsRequired();
            entity.Property(k => k.KeyHash).IsRequired();
            entity.HasIndex(k => k.KeyHash).IsUnique();

            entity.HasMany(k => k.RefreshTokens)
                .WithOne(t => t.ApiKey)
                .HasForeignKey(t => t.ApiKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TokenHash).IsRequired();
            entity.Property(t => t.OrganisationReference).IsRequired();
            entity.HasIndex(t => t.TokenHash).IsUnique();
        });
    }
}
