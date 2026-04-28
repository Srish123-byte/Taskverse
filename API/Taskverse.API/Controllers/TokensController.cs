using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Taskverse.Api.Controllers;

[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class TokensController : Controller
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokensController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [HttpGet("refresh")]
    [SwaggerResponse(200, Description = "Returns a refreshed token")]
    [SwaggerResponse(401, Description = "Authorization token is missing, invalid, or expired")]
    public IActionResult GetRefreshToken()
    {
        var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized("Authorization token is missing or invalid");
        }

        var rawToken = authorizationHeader["Bearer ".Length..].Trim();

        JwtSecurityToken? jwtToken;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            jwtToken = handler.ReadJwtToken(rawToken);
        }
        catch
        {
            return Unauthorized("Authorization token is missing or invalid");
        }

        if (jwtToken.ValidTo < DateTime.UtcNow)
        {
            return Unauthorized("Token has expired");
        }

        return Ok(rawToken);
    }
}
