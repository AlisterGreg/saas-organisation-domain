using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Saas.OrganisationDomain.Api.Auth.Domain;
using Saas.OrganisationDomain.Api.Persistence;

namespace Saas.OrganisationDomain.Api.Auth.Services;

/// <summary>Issues, validates, and rotates single-use refresh tokens.</summary>
public class RefreshTokenService(AuthDbContext db, IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    /// <summary>Refresh-token lifetime in seconds.</summary>
    public int RefreshTokenLifetimeSeconds => _options.RefreshTokenDays * 24 * 60 * 60;

    /// <summary>
    /// Issues a new refresh token for <paramref name="apiKey"/>, persists its
    /// hash, and returns the raw (opaque) token. Does not call SaveChanges when
    /// <paramref name="saveChanges"/> is <c>false</c> so it can participate in a
    /// rotation that saves once.
    /// </summary>
    public async Task<string> IssueAsync(ApiKey apiKey, bool saveChanges = true, CancellationToken cancellationToken = default)
    {
        var rawToken = TokenHashing.GenerateSecret();
        var now = DateTimeOffset.UtcNow;

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = TokenHashing.Hash(rawToken),
            ApiKeyId = apiKey.Id,
            OrganisationReference = apiKey.OrganisationReference,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.RefreshTokenDays)
        });

        if (saveChanges)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return rawToken;
    }

    /// <summary>
    /// Validates and rotates a refresh token. On success the presented token is
    /// revoked, a fresh token is issued, and the owning <see cref="ApiKey"/> plus
    /// the new raw token are returned. Returns <c>null</c> on any failure (not
    /// found, expired, owning key revoked); presenting an already-revoked token is
    /// treated as reuse and revokes the whole rotation chain for that key.
    /// </summary>
    public async Task<RotationResult?> ValidateAndRotateAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var tokenHash = TokenHashing.Hash(rawToken);
        var stored = await db.RefreshTokens
            .Include(t => t.ApiKey)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is null)
        {
            return null;
        }

        // Reuse: an already-revoked token is being presented. Revoke the entire
        // active chain for the owning key as a defensive measure.
        if (stored.RevokedAt is not null)
        {
            await RevokeChainAsync(stored.ApiKeyId, cancellationToken);
            return null;
        }

        if (stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        if (stored.ApiKey is null || stored.ApiKey.RevokedAt is not null)
        {
            return null;
        }

        // Rotate: issue a replacement, then revoke the presented token and link it
        // to its successor. Save once for the whole rotation.
        var newRawToken = await IssueAsync(stored.ApiKey, saveChanges: false, cancellationToken);
        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenHash = TokenHashing.Hash(newRawToken);

        await db.SaveChangesAsync(cancellationToken);

        return new RotationResult(stored.ApiKey, newRawToken);
    }

    private async Task RevokeChainAsync(Guid apiKeyId, CancellationToken cancellationToken)
    {
        var active = await db.RefreshTokens
            .Where(t => t.ApiKeyId == apiKeyId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var token in active)
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public record RotationResult(ApiKey ApiKey, string NewRefreshToken);
}
