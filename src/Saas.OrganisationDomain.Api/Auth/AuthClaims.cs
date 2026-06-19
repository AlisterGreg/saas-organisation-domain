namespace Saas.OrganisationDomain.Api.Auth;

/// <summary>Custom JWT claim types used by the API.</summary>
public static class AuthClaims
{
    /// <summary>The organisation (tenant) reference the token is scoped to.</summary>
    public const string Organisation = "org";
}
