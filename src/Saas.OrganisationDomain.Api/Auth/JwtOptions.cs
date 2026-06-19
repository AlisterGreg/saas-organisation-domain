namespace Saas.OrganisationDomain.Api.Auth;

/// <summary>Strongly-typed binding for the <c>Jwt</c> configuration section.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "saas-organisation-domain";

    public string Audience { get; set; } = "saas-organisation-domain";

    public string Secret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}
