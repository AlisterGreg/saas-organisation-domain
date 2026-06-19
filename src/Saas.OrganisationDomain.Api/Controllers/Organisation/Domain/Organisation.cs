namespace Saas.OrganisationDomain.Api.Controllers.Organisation.Domain;

public record Organisation(
    string Reference,
    string Name,
    string? TradingName,
    OrganisationType Type,
    OrganisationStatus Status,
    string? RegistrationNumber,
    string? TaxNumber,
    DateOnly? IncorporationDate,
    Address RegisteredAddress,
    Address? TradingAddress,
    ContactDetails Contact,
    string? Industry,
    int? EmployeeCount,
    decimal? AnnualRevenue,
    string? Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
