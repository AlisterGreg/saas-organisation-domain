using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Saas.OrganisationDomain.Api.Auth;
using Saas.OrganisationDomain.Api.Controllers.Organisation.Services;

namespace Saas.OrganisationDomain.Api.Controllers.Organisation;

[ApiController]
[Authorize]
public class OrganisationsController(
    GetOrganisationService getOrganisationService,
    CreateOrganisationService createOrganisationService) : ControllerBase
{
    [HttpGet("api/organisation/{reference}")]
    public IActionResult Get(string reference)
    {
        if (!User.MatchesOrganisation(reference))
        {
            return Forbid();
        }

        return Ok(getOrganisationService.Get(reference));
    }

    [HttpPost("api/organisation")]
    public IActionResult Post([FromBody] Domain.Organisation organisation)
    {
        if (!User.MatchesOrganisation(organisation.Reference))
        {
            return Forbid();
        }

        var created = createOrganisationService.Create(organisation);
        return CreatedAtAction(nameof(Get), new { reference = created.Reference }, created);
    }
}
