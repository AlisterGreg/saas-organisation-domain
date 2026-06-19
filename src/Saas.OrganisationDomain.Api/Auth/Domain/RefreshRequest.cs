namespace Saas.OrganisationDomain.Api.Auth.Domain;

/// <summary>Body of <c>POST /api/auth/refresh</c>.</summary>
public record RefreshRequest(string RefreshToken);
