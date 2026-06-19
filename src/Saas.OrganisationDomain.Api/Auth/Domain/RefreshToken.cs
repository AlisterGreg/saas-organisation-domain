namespace Saas.OrganisationDomain.Api.Auth.Domain;

/// <summary>
/// A single-use refresh token. Only the SHA-256 hash of the raw token is
/// persisted. Tokens are rotated on every refresh; the rotation chain
/// (<see cref="ReplacedByTokenHash"/>) enables reuse detection.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    /// <summary>SHA-256 (hex) of the raw refresh token. Unique.</summary>
    public required string TokenHash { get; set; }

    /// <summary>Owning API key / organisation.</summary>
    public Guid ApiKeyId { get; set; }

    public ApiKey? ApiKey { get; set; }

    /// <summary>Denormalised from the owning key for fast claim issuance.</summary>
    public required string OrganisationReference { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Set on rotation, logout, or reuse detection.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Hash of the token that replaced this one (rotation chain link).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
