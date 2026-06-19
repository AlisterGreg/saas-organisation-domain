using System.Security.Claims;

namespace Saas.OrganisationDomain.Api.Auth;

/// <summary>Per-tenant access checks against the JWT <c>org</c> claim.</summary>
public static class TenantAuthorization
{
    /// <summary>
    /// Returns <c>true</c> when the caller's <c>org</c> claim matches
    /// <paramref name="organisationReference"/>. Used to enforce that a token
    /// issued for one organisation cannot access another's resources.
    /// </summary>
    public static bool MatchesOrganisation(this ClaimsPrincipal user, string organisationReference)
    {
        var org = user.FindFirstValue(AuthClaims.Organisation);
        return org is not null && string.Equals(org, organisationReference, StringComparison.Ordinal);
    }
}
