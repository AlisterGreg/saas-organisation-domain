namespace Saas.OrganisationDomain.Api.Controllers.Organisation.Services;

public class GetOrganisationService
{
    public Domain.Organisation Get(string reference)
    {
        return new Domain.Organisation(
            Reference: reference,
            Name: string.Empty,
            TradingName: null,
            Type: Domain.OrganisationType.Company,
            Status: Domain.OrganisationStatus.Active,
            RegistrationNumber: null,
            TaxNumber: null,
            IncorporationDate: null,
            RegisteredAddress: new Domain.Address(string.Empty, null, string.Empty, null, string.Empty, string.Empty),
            TradingAddress: null,
            Contact: new Domain.ContactDetails(null, null, null),
            Industry: null,
            EmployeeCount: null,
            AnnualRevenue: null,
            Currency: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);
    }
}
