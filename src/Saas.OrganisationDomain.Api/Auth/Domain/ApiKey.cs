namespace Saas.OrganisationDomain.Api.Auth.Domain;

/// <summary>
/// An organisation-scoped API key. Only the SHA-256 hash of the raw key is
/// persisted; the plaintext is never stored. Keys are seeded, not self-service.
/// </summary>
public class ApiKey
{
    public Guid Id { get; set; }

    /// <summary>Tenant this key belongs to; becomes the JWT <c>org</c> claim.</summary>
    public required string OrganisationReference { get; set; }

    /// <summary>SHA-256 (hex) of the raw key. Unique.</summary>
    public required string KeyHash { get; set; }

    /// <summary>Human-readable name for the key.</summary>
    public string? Label { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Non-null ⇒ the key is revoked and must be rejected.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
