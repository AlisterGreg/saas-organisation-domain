namespace Saas.OrganisationDomain.Api.Controllers.Organisation.Domain;

public record Address(
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string PostalCode,
    string Country);
