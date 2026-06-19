using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Saas.OrganisationDomain.Api.OpenApi;

/// <summary>
/// Adds a JWT Bearer security scheme to the OpenAPI document so access tokens
/// from <c>POST /api/auth/token</c> can be sent from the API explorer.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the access token returned by POST /api/auth/token."
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = new List<string>()
        });

        return Task.CompletedTask;
    }
}
