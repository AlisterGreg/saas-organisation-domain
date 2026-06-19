using System.Security.Cryptography;
using System.Text;

namespace Saas.OrganisationDomain.Api.Auth.Services;

/// <summary>
/// SHA-256 hashing and high-entropy secret generation for API keys and refresh
/// tokens. API keys and refresh tokens are random secrets, so an unsalted
/// SHA-256 is sufficient — lookups are by hash, never by plaintext.
/// </summary>
public static class TokenHashing
{
    /// <summary>Returns the lowercase hex SHA-256 hash of <paramref name="raw"/>.</summary>
    public static string Hash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(bytes);
    }

    /// <summary>Generates a 256-bit cryptographically random Base64Url secret.</summary>
    public static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
