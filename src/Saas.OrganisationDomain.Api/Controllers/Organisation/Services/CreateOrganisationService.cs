namespace Saas.OrganisationDomain.Api.Controllers.Organisation.Services;

public class CreateOrganisationService
{
    public Domain.Organisation Create(Domain.Organisation organisation)
    {
        return organisation with
        {
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
