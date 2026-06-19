using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Saas.OrganisationDomain.Api.Auth.Domain;
using Saas.OrganisationDomain.Api.Auth.Services;

namespace Saas.OrganisationDomain.Api.Auth;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController(
    ApiKeyValidator apiKeyValidator,
    JwtTokenService jwtTokenService,
    RefreshTokenService refreshTokenService) : ControllerBase
{
    private const string ApiKeyHeader = "X-Api-Key";

    /// <summary>Exchanges a seeded <c>X-Api-Key</c> for an access + refresh token pair.</summary>
    [HttpPost("token")]
    public async Task<IActionResult> Token(CancellationToken cancellationToken)
    {
        var rawKey = Request.Headers[ApiKeyHeader].ToString();
        var apiKey = await apiKeyValidator.ValidateAsync(rawKey, cancellationToken);
        if (apiKey is null)
        {
            return Unauthorized();
        }

        var refreshToken = await refreshTokenService.IssueAsync(apiKey, cancellationToken: cancellationToken);
        return Ok(BuildResponse(jwtTokenService.CreateAccessToken(apiKey), refreshToken));
    }

    /// <summary>Rotates a refresh token, returning a fresh access + refresh pair.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await refreshTokenService.ValidateAndRotateAsync(request.RefreshToken, cancellationToken);
        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(BuildResponse(jwtTokenService.CreateAccessToken(result.ApiKey), result.NewRefreshToken));
    }

    private TokenResponse BuildResponse(string accessToken, string refreshToken) => new(
        AccessToken: accessToken,
        ExpiresIn: jwtTokenService.AccessTokenLifetimeSeconds,
        RefreshToken: refreshToken,
        RefreshExpiresIn: refreshTokenService.RefreshTokenLifetimeSeconds);
}
