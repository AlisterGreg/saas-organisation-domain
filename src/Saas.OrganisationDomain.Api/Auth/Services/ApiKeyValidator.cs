using Microsoft.EntityFrameworkCore;
using Saas.OrganisationDomain.Api.Auth.Domain;
using Saas.OrganisationDomain.Api.Persistence;

namespace Saas.OrganisationDomain.Api.Auth.Services;

/// <summary>Resolves a raw <c>X-Api-Key</c> to a non-revoked <see cref="ApiKey"/>.</summary>
public class ApiKeyValidator(AuthDbContext db)
{
    /// <summary>
    /// Returns the matching, non-revoked API key, or <c>null</c> if the key is
    /// missing, unknown, or revoked.
    /// </summary>
    public async Task<ApiKey?> ValidateAsync(string? rawKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return null;
        }

        var keyHash = TokenHashing.Hash(rawKey);
        var apiKey = await db.ApiKeys.SingleOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);

        return apiKey is { RevokedAt: null } ? apiKey : null;
    }
}
