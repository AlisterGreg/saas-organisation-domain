namespace Saas.OrganisationDomain.Api.Auth.Domain;

/// <summary>Token pair returned by the auth endpoints.</summary>
public record TokenResponse(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    int RefreshExpiresIn,
    string TokenType = "Bearer");
