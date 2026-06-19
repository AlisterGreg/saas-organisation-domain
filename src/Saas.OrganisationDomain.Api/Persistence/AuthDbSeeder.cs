using Microsoft.EntityFrameworkCore;
using Saas.OrganisationDomain.Api.Auth.Domain;
using Saas.OrganisationDomain.Api.Auth.Services;

namespace Saas.OrganisationDomain.Api.Persistence;

/// <summary>
/// Seeds a single development organisation API key when the table is empty.
/// The plaintext key below is for local testing only (documented in the README
/// and the .http file); only its SHA-256 hash is persisted.
/// </summary>
public static class AuthDbSeeder
{
    public const string DevOrganisationReference = "ORG-001";
    public const string DevApiKey = "sk_test_dev_organisation_001";

    public static async Task SeedAsync(AuthDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.ApiKeys.AnyAsync(cancellationToken))
        {
            return;
        }

        db.ApiKeys.Add(new ApiKey
        {
            Id = Guid.NewGuid(),
            OrganisationReference = DevOrganisationReference,
            KeyHash = TokenHashing.Hash(DevApiKey),
            Label = "Dev seed key",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
